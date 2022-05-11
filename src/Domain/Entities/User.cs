using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities
{
    [Index(nameof(Email), IsUnique = true)]
    public class User/* : IdentityUser*/
    {
        public /*string*/ int UserId { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "The firstname is too short")]
        [MaxLength(40, ErrorMessage = "The firstname is too long")]
        public string FirstName { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "The firstname is too short")]
        [MaxLength(50, ErrorMessage = "The firstname is too long")]
        public string LastName { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "The email is too short")]
        [MaxLength(50, ErrorMessage = "The email is too long")]
        public /*override*/ string Email { get; set; }

        [Required]
        [Range(0, 120, ErrorMessage = "The age needs to in bounds of 0 and 120")]
        public int Age { get; set; }

        [Required]
        public DateTime AccountCreation { get; set; }

        
        public IEnumerable<Book> Books { get; set; }
    }
}