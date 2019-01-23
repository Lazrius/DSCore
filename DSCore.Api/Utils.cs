using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;

namespace DSCore.Api
{
    internal static class Utils 
    {
        internal static bool CheckDatabase()
        {
            if (System.IO.File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FLData.db")))
                return true;

            return false;
        }

        internal static string GetDatabase()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FLData.db");
        }

        internal static string ReturnJson(object obj, params Errors[] errors)
        {
            List<string> stringErrors = new List<string>();
            foreach (var item in errors)
                stringErrors.Add(item.ToString());
            return JsonConvert.SerializeObject(new JsonWrapper() { Result = obj, Errors = stringErrors }, Formatting.Indented);
        }
    }

    public enum Errors
    {
        DatabaseNotFound,
        InvalidDatabaseStructure,
        ValueNotFound,
        Null
    }
}
