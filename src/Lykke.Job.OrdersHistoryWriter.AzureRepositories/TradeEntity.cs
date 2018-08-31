using System;
using System.Linq;
using Lykke.MatchingEngine.Connector.Models.Events;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    public class TradeEntity : TableEntity
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public string LimitOrderId { get; set; }
        public string OppositeLimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
        public double Price { get; set; }
        public string AssetId { get; set; }
        public string OppositeAssetId { get; set; }
        public double Volume { get; set; }
        public string ClientId { get; set; }
        public bool? IsLimitOrderResult { get; set; }
        public double FeeSize { get; set; }
        public string FeeTypeText { get; set; }

        public static string GenerateRowKey(string tradeId)
        {
            return tradeId;
        }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static TradeEntity FromMeModel(
                Trade trade,
                string walletId,
                string orderId,
                bool isLimitOrder)
            {
                var result = CreateNew(
                    trade,
                    walletId,
                    orderId,
                    isLimitOrder);
                result.PartitionKey = GeneratePartitionKey(walletId);
                result.RowKey = GenerateRowKey(trade.TradeId);
                return result;
            }
        }

        public static class ByDate
        {
            public static string GeneratePartitionKey(DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }

            public static TradeEntity FromMeModel(
                Trade trade,
                string walletId,
                string orderId,
                bool isLimitOrder)
            {
                var result = CreateNew(
                    trade,
                    walletId,
                    orderId,
                    isLimitOrder);
                result.PartitionKey = GeneratePartitionKey(trade.Timestamp);
                result.RowKey = GenerateRowKey(trade.TradeId);
                return result;
            }
        }

        public static class ByOrder
        {
            public static string GeneratePartitionKey(string orderId)
            {
                return orderId;
            }

            public static TradeEntity FromMeModel(
                Trade trade,
                string walletId,
                string orderId,
                bool isLimitOrder)
            {
                var result = CreateNew(
                    trade,
                    walletId,
                    orderId,
                    isLimitOrder);
                result.PartitionKey = GeneratePartitionKey(orderId);
                result.RowKey = GenerateRowKey(trade.TradeId);
                return result;
            }
        }

        private static TradeEntity CreateNew(
            Trade trade,
            string walletId,
            string orderId,
            bool isLimitOrder)
        {
            var result = new TradeEntity
            {
                Id = trade.TradeId,
                ClientId = walletId,
                AssetId = trade.BaseAssetId,
                OppositeAssetId = trade.QuotingAssetId,
                IsLimitOrderResult = isLimitOrder,
                DateTime = trade.Timestamp,
                Volume = double.Parse(trade.BaseVolume),
                Price = double.Parse(trade.Price),
            };
            var fee = trade.Fees?.FirstOrDefault(f => f.AssetId == trade.BaseAssetId);
            if (fee != null)
                result.FeeSize = double.Parse(fee.Volume);

            if (isLimitOrder)
            {
                result.IsLimitOrderResult = true;
                result.LimitOrderId = orderId;
                result.OppositeLimitOrderId = trade.OppositeExternalOrderId ?? trade.OppositeOrderId;
            }
            else
            {
                result.MarketOrderId = orderId;
                result.LimitOrderId = trade.OppositeExternalOrderId ?? trade.OppositeOrderId;
            }

            return result;
        }
    }
}
