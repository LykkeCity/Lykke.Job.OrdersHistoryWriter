using System;
using Lykke.MatchingEngine.Connector.Models.Events;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    public class LimitOrderEntity : TableEntity
    {
        public string Id { get; set; }
        public string MatchingId { get; set; }
        public string ClientId { get; set; }
        public double? Price { get; set; }
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public string Status { get; set; }
        public bool Straight { get; set; }
        public double RemainingVolume { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Registered { get; set; }
        public DateTime? MatchedAt { get; set; }

        public static string GenerateRowKey(string orderId)
        {
            return orderId;
        }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static LimitOrderEntity FromMeModel(Order limitOrder)
            {
                var entity = CreateNew(limitOrder);
                entity.PartitionKey = GeneratePartitionKey(limitOrder.WalletId);
                entity.RowKey = GenerateRowKey(limitOrder.ExternalId ?? limitOrder.Id);
                return entity;
            }
        }

        public static class ByDate
        {
            public static string GeneratePartitionKey(DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }

            public static LimitOrderEntity FromMeModel(Order limitOrder)
            {
                var entity = CreateNew(limitOrder);
                entity.PartitionKey = GeneratePartitionKey(limitOrder.CreatedAt);
                entity.RowKey = GenerateRowKey(limitOrder.ExternalId ?? limitOrder.Id);
                return entity;
            }
        }

        private static LimitOrderEntity CreateNew(Order limitOrder)
        {
            return new LimitOrderEntity
            {
                Id = limitOrder.ExternalId ?? limitOrder.Id,
                MatchingId = limitOrder.Id,
                AssetPairId = limitOrder.AssetPairId,
                ClientId = limitOrder.WalletId,
                Price = string.IsNullOrWhiteSpace(limitOrder.Price) ? (double?)null : double.Parse(limitOrder.Price),
                Status = limitOrder.Status.ToString(),
                Straight = limitOrder.Straight,
                Volume = double.Parse(limitOrder.Volume),
                RemainingVolume = double.Parse(limitOrder.RemainingVolume),
                CreatedAt = limitOrder.CreatedAt,
                Registered = limitOrder.Registered,
                MatchedAt = limitOrder.LastMatchTime,
            };
        }
    }
}
