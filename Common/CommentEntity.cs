using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CommentEntity : TableEntity
    {
        public string CreatorEmail { get; set; }

        // Movie metadata
        public string AuthorEmail { get; set; }
        public string Text { get; set; }
        public DateTime CreatedUtc { get; set; }

        public CommentEntity(string discussionId, string commentId)
        {
            PartitionKey = discussionId;
            RowKey = commentId;
        }

        public CommentEntity() { }
    }
}
