using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace a
{
    class Category
    {
        [JsonProperty("cid")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }

        private static int _lastId = 3;

        public static List<Category> Data = new List<Category>
        {
            new Category() { Id = 1, Name = "Beverages"},
            new Category() { Id = 2, Name = "Condiments"},
            new Category() { Id = 3, Name = "Confections"}
        };

        public static Category Create(string name)
        {
            var cat = new Category()
            {
                Id = ++_lastId,
                Name = name
            };
            Data.Add(cat);

            return cat;
        }
    }

}
