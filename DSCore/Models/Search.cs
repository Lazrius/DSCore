using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSCore.Ini;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DSCore.Models
{
    public class Search
    {
        public readonly string Nickname;
        public readonly string Name;
        public readonly string Infocard;
        public readonly string Type;

        public static Search CreateInstance<T>(T instance, List<Infocard> infocards)
        {
            PropertyInfo pi = instance.GetType().GetProperty("Name");
            PropertyInfo pi2 = instance.GetType().GetProperty("Infocard");
            PropertyInfo pi3 = instance.GetType().GetProperty("Nickname");
            if (pi is null || pi2 is null || pi3 is null) return null;
            uint idsName = (uint)(pi.GetValue(instance, null));
            uint idsInfo = (uint)(pi2.GetValue(instance, null));
            string nickname = (string)(pi3.GetValue(instance, null));
            string name, infocard;
            if (idsName != 0 && idsInfo != 0)
            {
                try
                {
                    name = infocards.FirstOrDefault(x => x.Key == idsName).Value;
                    infocard = infocards.FirstOrDefault(x => x.Key == idsInfo).Value;
                }
                catch
                {
                    return null;
                }
            }

            else
                return null;

            if (nickname is null || name is null || infocard is null)
                return null;
            Search search = new Search(nickname, name, infocard, instance.GetType().Name);
            return search;
        }

        private Search(string nickname, string name, string infocard, string type)
        {
            Nickname = nickname;
            Name = name;
            Infocard = infocard;
            Type = type;
        }
    }
}
