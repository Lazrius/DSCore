using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSCore.Api
{
    public class JsonWrapper
    {
        public List<string> Errors { get; set; }
        public object Result { get; set; }
    }
}
