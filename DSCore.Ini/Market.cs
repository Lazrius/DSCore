using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class Market
    {
        [BsonId]
        public string Base { get; set; }

        // Commodity Nick Name, Price multiplier
        public Dictionary<string, decimal> Good { get; set; }
    }
}
