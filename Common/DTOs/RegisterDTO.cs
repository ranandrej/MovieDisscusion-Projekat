using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTOs
{
    public class RegisterDTO
    {
        [Required, StringLength(80)]
        public string FullName { get; set; }

        [Required]                           // biće biran iz dropdown-a
        public string Gender { get; set; }   // "Male", "Female", "Other"

        [Required, StringLength(60)]
        public string Country { get; set; }

        [Required, StringLength(60)]
        public string City { get; set; }

        [Required, StringLength(120)]
        public string Address { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Url(ErrorMessage = "Must be a valid URL")]
        public string PhotoUrl { get; set; } // može i [Required] ako želiš da bude obavezna
    }
}
