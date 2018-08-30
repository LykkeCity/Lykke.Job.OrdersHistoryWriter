using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    internal class BatchSaver<T> : TimerPeriod, IStartStop
        where T : TableEntity
    {
        private const int _tableServiceBatchMaximumOperations = 100;
        private const int _maxNumberOfTasks = 50; //200
        private const int _warningPartitionsCount = 1000;
        private const int _warningPartitionQueueCount = 1000;

        private readonly CloudTable _table;
        private readonly ILog _log;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private Dictionary<string, List<T>> _bufferDict = new Dictionary<string, List<T>>();

        public BatchSaver(
            string connectionString,
            string tableName,
            ILogFactory logFactory)
            : base(TimeSpan.FromMilliseconds(50), logFactory)
        {
            var cloudAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = cloudAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(tableName);
            _log = logFactory.CreateLog(this);
        }

        public async Task AddAsync(params T[] items)
        {
            await _lock.WaitAsync();
            try
            {
                foreach (var item in items)
                {
                    if (_bufferDict.ContainsKey(item.PartitionKey))
                    {
                        var partitionQueue = _bufferDict[item.PartitionKey];
                        partitionQueue.Add(item);
                        if (partitionQueue.Count >= _warningPartitionQueueCount)
                            _log.Warning($"Partition {item.PartitionKey} queue has {partitionQueue.Count} items");
                    }
                    else
                    {
                        _bufferDict.Add(item.PartitionKey, new List<T> { item });
                        if (_bufferDict.Count >= _warningPartitionsCount)
                            _log.Warning($"Buffer has {_bufferDict.Count} partitions");
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public override void Stop()
        {
            base.Stop();

            Execute().GetAwaiter().GetResult();
        }

        public override async Task Execute()
        {
            Dictionary<string, List<T>> bufferDict;
            await _lock.WaitAsync();
            try
            {
                if (_bufferDict.Count == 0)
                    return;

                bufferDict = _bufferDict;
                _bufferDict = new Dictionary<string, List<T>>();
            }
            finally
            {
                _lock.Release();
            }

            int taskCount = 0;
            var batchTasks = new List<Task<IList<TableResult>>>();

            try
            {
                foreach (var partitionItems in bufferDict.Values)
                {
                    for (var i = 0; i < partitionItems.Count; i += _tableServiceBatchMaximumOperations)
                    {
                        var batchItems = partitionItems.GetRange(i, Math.Min(_tableServiceBatchMaximumOperations, partitionItems.Count - i));

                        var batchOp = new TableBatchOperation();
                        foreach (var item in batchItems)
                        {
                            batchOp.InsertOrMerge(item);
                        }

                        var task = _table.ExecuteBatchAsync(batchOp);
                        batchTasks.Add(task);
                        ++taskCount;

                        if (taskCount >= _maxNumberOfTasks)
                        {
                            await Task.WhenAll(batchTasks);
                            batchTasks.Clear();
                            taskCount = 0;
                        }
                    }
                }

                if (batchTasks.Count > 0)
                    await Task.WhenAll(batchTasks);
            }
            catch (Exception exc)
            {
                _log.Error(exc);
                throw;
            }
        }
    }
}
