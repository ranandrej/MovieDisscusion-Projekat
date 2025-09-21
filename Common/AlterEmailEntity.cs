using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class AlterEmailEntity
    {
        // Table: AlertEmails
        // PK = "Alert"
        // RK = emailLower
        public class AlertEmailEntity : TableEntity
        {
            public AlertEmailEntity(string emailLower)
            {
                PartitionKey = "Alert";
                RowKey = emailLower;
                // Email = emailLower; // čuvamo i originalnu vrednost
            }

            public AlertEmailEntity() { }

            // public string Email { get; set; }
        }
    }
}
