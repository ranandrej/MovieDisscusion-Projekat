using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class NotificationLogEntity : TableEntity
    {
        public DateTime TimestampUtc { get; set; }
        public int SentCount { get; set; }

        public NotificationLogEntity(string commentId, DateTime timestampUtc, int sentCount)
        {
            PartitionKey = "Log";
            RowKey = commentId;
            TimestampUtc = timestampUtc;
            SentCount = sentCount;
        }

        public NotificationLogEntity() { }
    }
}
