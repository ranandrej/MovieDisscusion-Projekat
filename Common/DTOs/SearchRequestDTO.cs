using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs
{
    public class SearchRequestDTO
    {
        public enum SortBy
        {
            None = 0,
            ScoreDesc,
            ScoreAsc,
        }

        public class SearchRequest
        {
            public string TitleContains { get; set; }
            public string GenreEquals { get; set; }
            public SortBy SortBy { get; set; }

            // Paginacija
            public int Page { get; set; } = 1;          // Trenutna stranica
            public int PageSize { get; set; } = 4;     // Broj diskusija po stranici
        }
    }

}
