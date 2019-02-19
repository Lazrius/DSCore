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
        public List<MarketGood> Goods { get; set; }
    }

    public sealed class MarketGood
    {
        public string Nickname { get; set; }
        public int StockA { get; set; }
        public int StockB { get; set; }
        public decimal PriceModifier { get; set; }
    }
}
