using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class DiscussionEntity : TableEntity
    {
        public string CreatorEmail { get; set; }

        // Movie metadata
        public string MovieTitle { get; set; }
        public int Year { get; set; }
        public string Genre { get; set; }
        public double ImdbRating { get; set; }
        public string Synopsis { get; set; }
        public int DurationMin { get; set; }
        public string PosterUrl { get; set; }

        // Reactions (aggregate)
        public int Positive { get; set; }
        public int Negative { get; set; }

        // For listing/sorting
        public DateTime CreatedUtc { get; set; }

        public DiscussionEntity(string discussionId)
        {
            PartitionKey = "Disc";
            RowKey = discussionId;  // e.g., Guid.NewGuid().ToString("N")
        }

        public DiscussionEntity() { }
    }
    
}
    