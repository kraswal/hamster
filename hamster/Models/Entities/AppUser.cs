using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hamster.Models.Entities
{
    public class AppUser: IdentityUser<int>
    {
        public string Password { get; set; }
        public string Nickname { get; set; }
        public bool IsAdmin { get; set; }
    }
}
