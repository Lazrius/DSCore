using DSCore.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSCore.Ini;
using LiteDB;

namespace DSCore.Utilities
{
    public enum Errors
    {
        DatabaseNotFound,
        InvalidDatabaseStructure,
        ValueNotFound,
        ClassPropertiesDoNotMatch,
        Null
    }

    public static class Utils
    {
        private static string GetDatabase()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FLData.db");
        }

        private static bool CheckDatabase()
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FLData.db"));
        }

        public static T GetAPIResponse<T>(string targetEndpoint)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    string jsonString = webClient.DownloadString("https://localhost:3080/api/" + targetEndpoint);
                    JsonWrapper jsonObject = JsonConvert.DeserializeObject<JsonWrapper>(jsonString);
                    T result = JsonConvert.DeserializeObject<T>(jsonObject.Result.ToString());
                    return result;
                }
            }

            catch
            {
                return default(T);
            }
        }

        public static List<T> GetDatabaseCollection<T>(string collectionName, ref Errors error)
        {
            if (!CheckDatabase())
            {
                error = Errors.DatabaseNotFound;
                return default(List<T>);
            }

            using (var db = new LiteDatabase(GetDatabase()))
            {
                var collection = db.GetCollection<T>(collectionName);
                if (collection.Count() == 0)
                {
                    error = Errors.InvalidDatabaseStructure;
                    return default(List<T>);
                }

                var list = new List<T>();
                foreach (var i in collection.FindAll())
                    list.Add(i);
                db.Dispose();
                error = Errors.Null;
                return list;
            }
        }

        public static Dictionary<Base, decimal> GetSellPoint(string endpoint, string item)
        {
            List<Market> markets = GetAPIResponse<List<Market>>(endpoint);
            Dictionary<string, decimal> baseList = new Dictionary<string, decimal>();
            foreach (var i in markets)
            {
                if (i.Good.ContainsKey(item))
                    baseList.Add(i.Base, i.Good.FirstOrDefault(x => x.Key == item).Value);
            }

            Dictionary<Base, decimal> bases = new Dictionary<Base, decimal>();
            foreach (var s in baseList)
            {
                Base b = GetAPIResponse<Base>("base/" + s.Key);
                bases[b] = s.Value;
            }

            return bases;
        }

        public static string XmlToHtml(string xml)
        {
            Regex r = new Regex(@"<\?xml.*\?>");
            Match m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, "");
            r = new Regex(@"<JUST loc=\""center\""\/><TEXT>.*?<\/TEXT>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, "");
            r = new Regex(@"<TRA.*?\/>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, "");
            r = new Regex(@"<JUST.*?\/>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, @"");
            xml = xml.Replace("<RDL><PUSH/>", "");
            xml = xml.Replace("<POP/></RDL>", "");
            xml = xml.Replace("<PARA/>", "<br/>");
            xml = xml.Replace("<TEXT>", "<p>");
            xml = xml.Replace("</TEXT>", "</p>");
            r = new Regex(@"<(\w+)\s*.*?>\s*?</\1>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, @"");
            return xml;
        }

        public static string GetGridCode(Ini.System system, float[] position, bool displayVertical = false)
        {
            decimal scale = system.NavMapScale;
            // Logic: 
            // FL's NavMapScale divides the default value (which is 150k across the entire grid)
            // by the navmap scale. Stupid, I know.
            // So NavMapScale 1 = 30k per grid
            // and NavMapScale 2 = 15k per grid
            // and so on.

            float totalSize = 30000f * 8f /(float) scale;
            float gridSize = totalSize / 8f;
            float[] gridX = new [] { -600000f, -gridSize * 4, -gridSize * 3, -gridSize * 2, -gridSize, gridSize, gridSize * 2, gridSize * 3, gridSize * 4, 600000f };
            float[] gridZ = new[] { -600000f, -gridSize * 4, -gridSize * 3, -gridSize * 2, -gridSize, gridSize, gridSize * 2, gridSize * 3, gridSize * 4, 600000f };

            char xAxis = 'A';
            for (byte index = 0; index < gridX.Length; index++)
            {
                if (index + 1 >= gridX.Length)
                {
                    xAxis = 'H';
                    break;
                }
                var x1 = gridX[index];
                var x2 = gridX[index + 1];

                if (position[0] >= x1 && position[0] <= x2)
                {
                    xAxis = index == 0 ? 'A' : (char) Convert.ToByte(index + 64);
                    break;
                }
            }

            byte yAxis = 1;
            for (byte index = 0; index < gridZ.Length; index++)
            {
                if (index + 1 >= gridZ.Length)
                {
                    yAxis = 8;
                    break;
                }
                var y1 = gridX[index];
                var y2 = gridX[index + 1];

                if (position[2] >= y1 && position[2] <= y2)
                {
                    yAxis = index == 0 ? (byte)1 : index;
                    break;
                }
            }

            string code = xAxis.ToString() + yAxis.ToString();
            if (displayVertical)
            {
                float y = position[1] / 1000f;
                bool above = false;

                if (Math.Abs(y) < 1f && Math.Abs(y) > -1f)
                    return code;

                else if (y > 0)
                    above = true;

                code += $" ({(float)Math.Round(y)}k " + (above ? "above" : "below") + " plane)";
            }

            return code;
        }

        public enum Endpoints
        {
            commodity,
            good,
            infocard,
            weapon,
            faction,
            system
        }
    }
}
