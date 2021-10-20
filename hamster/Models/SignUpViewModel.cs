using System.ComponentModel.DataAnnotations;

namespace hamster.Models
{
    public class SignUpViewModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Nickname { get; set; }
    }
}
