using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class VoteEntity : TableEntity
    {
        public VoteEntity(string discussionId, string userEmail)
        {
            PartitionKey = discussionId;           // ID diskusije
            RowKey = userEmail?.ToLowerInvariant(); // Email korisnika
        }

        public VoteEntity() { }

        public bool IsLike { get; set; }  // Da li je glas "lajk" ili "dislajk"
    }
}
