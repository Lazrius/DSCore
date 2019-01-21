using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class Powerplant
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public int Capacity { get; set; }
        public int ChargeRate { get; set; }
    }
}
