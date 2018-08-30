using System;
using Lykke.MatchingEngine.Connector.Models.Events;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    public class MarketOrderEntity : TableEntity
    {
        public string Id { get; set; }
        public string MatchingId { get; set; }
        public string ClientId { get; set; }
        public double? Price { get; set; }
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public string Status { get; set; }
        public bool Straight { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? MatchedAt { get; set; }
        public DateTime? Registered { get; set; }

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

            public static MarketOrderEntity FromMeModel(Order marketOrder)
            {
                var entity = CreateNew(marketOrder);
                entity.PartitionKey = GeneratePartitionKey(marketOrder.WalletId);
                entity.RowKey = GenerateRowKey(marketOrder.ExternalId ?? marketOrder.Id);
                return entity;
            }
        }

        public static class ByDate
        {
            public static string GeneratePartitionKey(DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }

            public static MarketOrderEntity FromMeModel(Order marketOrder)
            {
                var entity = CreateNew(marketOrder);
                entity.PartitionKey = GeneratePartitionKey(marketOrder.CreatedAt);
                entity.RowKey = GenerateRowKey(marketOrder.ExternalId ?? marketOrder.Id);
                return entity;
            }
        }

        private static MarketOrderEntity CreateNew(Order marketOrder)
        {
            return new MarketOrderEntity
            {
                Id = marketOrder.ExternalId ?? marketOrder.Id,
                MatchingId = marketOrder.Id,
                AssetPairId = marketOrder.AssetPairId,
                ClientId = marketOrder.WalletId,
                Price = string.IsNullOrWhiteSpace(marketOrder.Price) ? (double?)null : double.Parse(marketOrder.Price),
                Status = marketOrder.Status.ToString(),
                Straight = marketOrder.Straight,
                Volume = double.Parse(marketOrder.Volume),
                CreatedAt = marketOrder.CreatedAt,
                Registered = marketOrder.Registered,
                MatchedAt = marketOrder.LastMatchTime,
            };
        }
    }
}
