using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs
{
    public class ReactDTO
    {
        // Enum za reakcije korisnika
        public enum ReactionType
        {
            Up,    // Lajk
            Down   // Dislajk
        }

        // DTO koji se koristi za reakcije korisnika
        public class ReactDto
        {
            public string DiscussionId { get; set; } // ID diskusije na koju korisnik reaguje
            public ReactionType Type { get; set; }   // Tip reakcije (Up/Down)
        }
    }
}
