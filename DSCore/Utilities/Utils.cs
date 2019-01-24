using DSCore.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DSCore.Utilities
{
    public static class Utils
    {
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

        public static string XmlToHtml(string xml)
        {
            Regex r = new Regex(@"<\?xml.*\?>");
            Match m = r.Match(xml);
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

        public enum Endpoints
        {
            commodity,
            good,
            infocard,
            weapon,
            faction
        }
    }
}
