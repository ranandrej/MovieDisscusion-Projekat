using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class SubscriptionEntity : TableEntity
    {
        public SubscriptionEntity() { }

        public SubscriptionEntity(string discussionId, string email)
        {
            PartitionKey = discussionId; // Sve pretplate grupisane po diskusiji
            RowKey = email;
            Email = email;
        }

        public string Email { get; set; }
    }
}
