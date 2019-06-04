using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB;

namespace DSCore.Utilities
{
    public enum Errors
    {
        DatabaseNotFound, // No data base file within the project
        InvalidDatabaseStructure, // The class type specified isn't the same as the collection we tried to access (or the db table is empty)
        ValueNotFound, // Unused for now
        ClassPropertiesDoNotMatch, // Unused for now
        Null // No Errors
    }

    public static class Utils
    {
        private static string GetDatabase()
        {
            // Return the database file path
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FLData.db");
        }

        private static bool CheckDatabase()
        {
            // If the database file doesn't exist return false
            return File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FLData.db"));
        }

        // Api stuff commeneted out as it's not needed currently
        /*public static T GetAPIResponse<T>(string targetEndpoint)
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
        }*/

        // This function will return the collection of the class type specified. The list will be null if invalid with an error type.
        public static List<T> GetDatabaseCollection<T>(string collectionName, ref Errors error)
        {
            if (!CheckDatabase()) // Does the database exist?
            {
                error = Errors.DatabaseNotFound; // We are missing the db file
                return default(List<T>); // Return null
            }

            using (var db = new LiteDatabase(GetDatabase())) // Establish connection to the database
            {
                var collection = db.GetCollection<T>(collectionName); // Attempt to get the collection by name and type
                if (collection.Count() == 0) // This will occur if the name is invalid or the type is invalid
                {
                    error = Errors.InvalidDatabaseStructure; // Set the error type
                    return default(List<T>); // Return null
                }

                var list = new List<T>(); // Otherwise we create a new list
                foreach (var i in collection.FindAll()) // Add every item from the database to the list
                    list.Add(i);
                db.Dispose(); // Dispose of our database object correctly
                error = Errors.Null; // No errors
                return list; // Return contents
            }
        }

        // This is a rather horrible function for converting FL's XML to valid HTML. It works, but it's a pretty nasty solution.
        // TODO: Consider reworking this freak of nature
        public static string XmlToHtml(string xml)
        {
            Regex r = new Regex(@"<\?xml.*\?>"); // Remove the XML header
            Match m = r.Match(xml);
            if (m.Success) // If the string had an XML header
                xml = xml.Replace(m.Value, ""); // Remove it
            r = new Regex(@"<JUST loc=\""center\""\/><TEXT>.*?<\/TEXT>"); // In 99% of instances, this is a title value.
            m = r.Match(xml); // If it has a title
            if (m.Success) 
                xml = xml.Replace(m.Value, ""); // Remove the title and alignment code
            r = new Regex(@"<JUST.*?\/>"); // If there is any other alignement code, we'll want it gone
            m = r.Match(xml);
            if (m.Success)
                xml = xml.Replace(m.Value, @""); // Remove all alignment code
            xml = xml.Replace("<RDL><PUSH/>", ""); // Redundant XML that breaks our formatting
            xml = xml.Replace("<POP/></RDL>", ""); // ^
            xml = xml.Replace("<PARA/>", "<br/>"); // Replace PARA tags with a line break
            xml = xml.Replace("<TEXT>", "<p>"); // Open new paragraph
            xml = xml.Replace("</TEXT>", "</p>"); // Close paraghraph
            r = new Regex(@"<(\w+)\b(?:\s+[\w\-.:]+(?:\s*=\s*(?:""[^ ""]*"" | ""[^""] * ""|[\w\-.:]+))?)*\s*\/?>\s*<\/\1\s*>");
            m = r.Match(xml); // This Regex will remove any other redundant xml tags that we don't care for
            if (m.Success)
                xml = xml.Replace(m.Value, @""); // Remove the stray elements if they exist
            r = new Regex(@"<TRA.*?\/>"); // TRA were always left over for some reason
            m = r.Match(xml); // So let's get rid of those as well
            if (m.Success)
                xml = xml.Replace(m.Value, "");
            return xml;
        }

        public static string GetGridCode(decimal navMapScale, float[] position, bool displayVertical = false)
        {
            if (navMapScale == 0) // Some systems don't have a scale value because by default FL assumes they have the max size of 150k
                navMapScale = 1;
            // Logic: 
            // FL's NavMapScale divides the default value (which is 150k across the entire grid)
            // by the navmap scale. Stupid, I know.
            // So NavMapScale 1 = 30k per grid
            // and NavMapScale 2 = 15k per grid
            // and so on.

            float totalSize = GetMapSize(navMapScale); // Get Total Grid Size
            float gridSize = totalSize / 8f; // Each individual grid will be the above value divided by the total amount of grids along the axis (8)
            float[] grid = new [] { -gridSize * 4, -gridSize * 3, -gridSize * 2, -gridSize, 0, gridSize, gridSize * 2, gridSize * 3, gridSize * 4 };
            // We create an array representing the smallest possible value and the maximum possible value

            // The Decimal value for a capital A is 65. So we want to start of with that.
            byte xAxis = 65; // A
            for (byte index = 0; index < grid.Length; index++) // Loop over the amount of grids (+1 for origin)
            {
                if (index + 1 == grid.Length) // If we have finished looping over the grids and still not found it (edge case for those located outside the map)
                {
                    xAxis = 72; // H
                    break;
                }
                var x1 = grid[index]; // The current grid code
                var x2 = grid[index + 1]; // The next grid code

                if (position[0] >= x1 && position[0] <= x2) // If we are between the two grid codes, e.g. between E-F
                {
                    xAxis = Convert.ToByte(index + xAxis); // A decimal equivalent of a letter between A-H 
                    break; // We found our x pos
                }
            }

            byte yAxis = 1; // The number 1
            for (byte index = 0; index < grid.Length; index++) // Loop 8 times.
            {
                if (index + 1 == grid.Length) // If we are on the last loop, set the value and return.
                {
                    yAxis = 8; // Max value
                    break;
                }
                var y1 = grid[index]; // The current grid code
                var y2 = grid[index + 1]; // The next grid code

                if (position[2] >= y1 && position[2] <= y2) // Are we between the two grid codes? e.g. between 4-5
                {
                    yAxis = (byte)(index + yAxis); // the yaxis doesn't need to be cast to a letter so we use numbers directly
                    break; // Found it
                }
            }

            // Edge case handling. If a base was below the minimum possible value, give it the right values
            if (grid[0] > position[0])
                xAxis = 65;

            if (grid[0] > position[2])
                yAxis = 1;

            // Convert the two bytes to a string that is a char, A-H, and a number, 1-8
            string code = ((char)xAxis) + yAxis.ToString();
            if (displayVertical) // If we are displaying the vertical pos value, we want to round it to the nearest 1000.
            {
                float y = position[1] / 1000f; // Get the amount of K they are above/below 
                bool above = false;

                if (Math.Abs(y) < 1f && Math.Abs(y) > -1f) // If it's more than -1000 and less than 1000, ignore it.
                    return code; // Move on

                else if (y > 0) // If it's positive, they are above plane
                    above = true;

                // Round it to the nearest whole number, then write whether it's above or below the plane.
                code += $" ({(float)Math.Round(y)}k " + (above ? "above" : "below") + " plane)";
            }

            return code; // return our code
        }

        // Get the total griz size
        public static float GetMapSize(decimal scale)
        {
            float totalSize = 30000f * 8f / (float)scale; // 150,000 / scale
            return totalSize;
        }
    }
}
