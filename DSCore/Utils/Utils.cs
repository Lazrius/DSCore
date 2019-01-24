using DSCore.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DSCore.Utils
{
    public static class API
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
