using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public class HealthCheckEntity : TableEntity
    {
        public HealthCheckEntity() { }

        public HealthCheckEntity(string serviceName, DateTime utcNow)
        {
            PartitionKey = serviceName;
            RowKey = utcNow.Ticks.ToString("d19"); // sort by time
            ServiceName = serviceName;
            TimestampUtc = utcNow;
        }

        public string ServiceName { get; set; }
        public string Status { get; set; } // OK / NOT_OK
        public DateTime TimestampUtc { get; set; }
    }
}
