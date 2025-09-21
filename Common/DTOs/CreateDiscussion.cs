using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs
{
    public class CreateDiscussionDTO
    {
        public string MovieTitle { get; set; }
        public int Year { get; set; }
        public string Genre { get; set; }
        public double ImdbRating { get; set; }
        public string Synopsis { get; set; }
        public int DurationMin { get; set; }
        public string PosterUrl { get; set; }

        // Polja za diskusiju
        public string DiscussionTitle { get; set; }
        public string DiscussionContent { get; set; }

        // Identifikacija korisnika preko emaila
        public string UserEmail { get; set; }
    }

}
