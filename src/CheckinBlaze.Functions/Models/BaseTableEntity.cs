using Azure;
using Azure.Data.Tables;
using System;

namespace CheckinBlaze.Functions.Models
{
    /// <summary>
    /// Base class for all Azure Table Storage entities
    /// </summary>
    public class BaseTableEntity : ITableEntity
    {
        /// <summary>
        /// Partition key for Azure Table Storage
        /// </summary>
        public string PartitionKey { get; set; }
        
        /// <summary>
        /// Row key for Azure Table Storage
        /// </summary>
        public string RowKey { get; set; }
        
        /// <summary>
        /// Timestamp for Azure Table Storage
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
        
        /// <summary>
        /// ETag for optimistic concurrency
        /// </summary>
        public ETag ETag { get; set; }
    }
}