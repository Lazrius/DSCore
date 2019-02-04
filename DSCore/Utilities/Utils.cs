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
            r = new Regex(@"<JUST.*?\/>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, @"");
            xml = xml.Replace("<RDL><PUSH/>", "");
            xml = xml.Replace("<POP/></RDL>", "");
            xml = xml.Replace("<PARA/>", "<br/>");
            xml = xml.Replace("<TEXT>", "<p>");
            xml = xml.Replace("</TEXT>", "</p>");
            r = new Regex(@"<(\w+)\b(?:\s+[\w\-.:]+(?:\s*=\s*(?:""[^ ""]*"" | ""[^""] * ""|[\w\-.:]+))?)*\s*\/?>\s*<\/\1\s*>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, @"");
            r = new Regex(@"<TRA.*?\/>");
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, "");
            return xml;
        }

        public static string GetGridCode(Ini.System system, float[] position, bool displayVertical = false)
        {
            decimal scale = system.NavMapScale;
            if (scale == 0) // Some systems don't have a scale value because by default FL assumes they have the max size of 150k
                scale = 1;
            // Logic: 
            // FL's NavMapScale divides the default value (which is 150k across the entire grid)
            // by the navmap scale. Stupid, I know.
            // So NavMapScale 1 = 30k per grid
            // and NavMapScale 2 = 15k per grid
            // and so on.

            float totalSize = 30000f * 8f /(float) scale;
            float gridSize = totalSize / 8f;
            float[] grid = new [] { -gridSize * 4, -gridSize * 3, -gridSize * 2, -gridSize, 0, gridSize, gridSize * 2, gridSize * 3, gridSize * 4 };

            // The Decimal value for a capital A is 65. So we want to start of with that.
            byte xAxis = 65; // A
            for (byte index = 0; index < grid.Length; index++)
            {
                

                if (index + 1 == grid.Length)
                {
                    xAxis = 72; // H
                    break;
                }
                var x1 = grid[index];
                var x2 = grid[index + 1];

                if (position[0] >= x1 && position[0] <= x2)
                {
                    xAxis = Convert.ToByte(index + xAxis); // A decimal equivalent of a letter between A-H 
                    break;
                }
            }

            byte yAxis = 1;
            for (byte index = 0; index < grid.Length; index++) // Loop 8 times.
            {
                if (index + 1 == grid.Length) // If we are on the last loop, set the value and return.
                {
                    yAxis = 8; // Max value
                    break;
                }
                var y1 = grid[index];
                var y2 = grid[index + 1]; 

                if (position[2] >= y1 && position[2] <= y2)
                {
                    yAxis = (byte)(index + yAxis);
                    break;
                }
            }

            if (grid[0] > position[0])
                xAxis = 65;

            if (grid[0] > position[2])
                yAxis = 1;

            // Convert the two bytes to a string that is a char, A-H, and a number, 1-8
            string code = ((char)xAxis) + yAxis.ToString();
            if (displayVertical) // If we are displaying the vertical pos value, we want to round it to the nearest 1000.
            {
                float y = position[1] / 1000f; 
                bool above = false;

                if (Math.Abs(y) < 1f && Math.Abs(y) > -1f) // If it's more than -1000 and less than 1000, ignore it.
                    return code; // Move on

                else if (y > 0)
                    above = true;

                // Round it to the nearest whole number, then write whether it's above or below the plane.
                code += $" ({(float)Math.Round(y)}k " + (above ? "above" : "below") + " plane)";
            }

            return code;
        }

        public static float GetMapSize(decimal scale)
        {
            float totalSize = 30000f * 8f / (float)scale;
            return totalSize;
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
