using System.Collections.Generic;
using LiteDB;

namespace DSCore.Ini
{
    public class Infocard
    {
        [BsonId]
        public uint Key { get; set; }
        public string Value { get; set; }
    }
}
