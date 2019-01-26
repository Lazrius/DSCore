using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public class System
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public decimal NavMapScale { get; set; }
        public string Region { get; set; }
        public List<Base> Bases { get; set; }
    }
}
