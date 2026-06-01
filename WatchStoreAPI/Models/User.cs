using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WatchStoreAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string Phone { get; set; }
        [MinLength(6)]
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "User";
        public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}