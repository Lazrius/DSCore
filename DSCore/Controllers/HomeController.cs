using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using DSCore.Ini;
using DSCore.Models;
using DSCore.Utilities;
using System.Dynamic;

namespace DSCore.Controllers
{
    [Route("")]
    [Route("[controller]")]
    public class HomeController : InheritController
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Search")]
        public IActionResult Search()
        {
            return View();
        }

       /*
        [Throttle(Name = "Search", Seconds = 5)]
        [HttpGet("Search/{queryString}")]
        public async Task<IActionResult> SearchResults(string queryString)
        {
            
        } */

        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /*private List<object> GetAllData(Dictionary<string, string[]> searchCriteria)
        {
            Errors error = Errors.Null;
            var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Bases collection.");

            var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Factions collection.");

            var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Infocards collection.");

            var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Systems collection.");

            var cms = Utils.GetDatabaseCollection<CountermeasureDropper>("CMs", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the CMs collection.");

            var weapons = Utils.GetDatabaseCollection<Weapon>("Weapons", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Commodities collection.");

            var shields = Utils.GetDatabaseCollection<Shield>("Shields", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Shields collection.");

            var ships = Utils.GetDatabaseCollection<Ship>("Ships", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Ships collection.");

            var thrusters = Utils.GetDatabaseCollection<Thruster>("Thrusters", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Thrusters collection.");

            var commodities = Utils.GetDatabaseCollection<Commodity>("Commodities", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Commodities collection.");

            var cloaks = Utils.GetDatabaseCollection<CloakingDevice>("Cloaks", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Cloaks collection.");

            var disrupters = Utils.GetDatabaseCollection<CloakDisrupter>("Disrupters", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Disrupters collection.");

            List<object> all = new List<object>();
            all.AddRange(cloaks);
            all.AddRange(disrupters);
            all.AddRange(cms);
            all.AddRange(commodities);
            all.AddRange(shields);
            all.AddRange(thrusters);
            all.AddRange(ships);
            all.AddRange(bases);
            all.AddRange(systems);
            all.AddRange(weapons);

            List<object> sorted = new List<object>();
            foreach (object obj in all)
            {
                PropertyInfo pi = obj.GetType().GetProperty("Name");
                PropertyInfo pi2 = obj.GetType().GetProperty("Infocard");
                PropertyInfo pi3 = obj.GetType().GetProperty("Nickname");
                uint idsName = (uint)(pi.GetValue(obj, null));
                uint idsInfo = (uint)(pi2.GetValue(obj, null));
                string nickname = (string)(pi3.GetValue(obj, null));
                string name, infocard;
                if (idsName != 0 && idsInfo != 0)
                {
                    try
                    {
                        name = infocards.FirstOrDefault(x => x.Key == idsName).Value;
                        infocard = infocards.FirstOrDefault(x => x.Key == idsInfo).Value;
                    }
                    catch { continue; }
                }

                else
                    continue;

                string[] toSearch = { nickname.ToLower(), name.ToLower(), infocard.ToLower() };
                if (toSearch.Contains(searchTerm.ToLower()))
                    sorted.Add(obj);

                if (sorted.Count >= 30)
                    break;

                return sorted;
            }
        } */
    }
}
