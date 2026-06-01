using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WatchStoreAPI.Models
{
    public class Category
    {
        public int Id { get; set; }   
        public string Name{ get; set; }
       [JsonIgnore]
      public ICollection<Product> ?Products { get; set; }
    }
}