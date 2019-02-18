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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DSCore.Controllers
{
    [Route("")]
    [Route("[controller]")]
    public class HomeController : InheritController
    {
        private readonly int _maxSearchResults = 100;
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Search")]
        public IActionResult Search()
        {
            return View();
        }

        [Throttle(Name = "Search", Seconds = 10)]
        [HttpGet("Search/{queryString}")]
        public IActionResult SearchResults(string queryString)
        {
            try
            {
                Regex regex = new Regex(@"^query=(.*?)((?:&t).*)");
                Match match = regex.Match(queryString);
                if (!match.Success)
                {
                    return View("SearchResults", new List<Search>());
                }

                string search = match.Groups[1].Value;
                string[] categories = match.Groups[2].Value.Split("&");
                bool[] includeArray = new bool[10];
                foreach (string str in categories)
                {
                    if (string.IsNullOrEmpty(str)) continue;
                    string category = str.Split("=")[1];
                    switch (category)
                    {
                        case "base":
                            includeArray[0] = true;
                            break;
                        case "com":
                            includeArray[1] = true;
                            break;
                        case "cms":
                            includeArray[2] = true;
                            break;
                        case "fac":
                            includeArray[3] = true;
                            break;
                        case "shld":
                            includeArray[4] = true;
                            break;
                        case "ship":
                            includeArray[5] = true;
                            break;
                        case "sequip":
                            includeArray[6] = true;
                            break;
                        case "sys":
                            includeArray[7] = true;
                            break;
                        case "thru":
                            includeArray[8] = true;
                            break;
                        case "weap":
                            includeArray[9] = true;
                            break;
                    }
                }

                return View("SearchResults", GetSearchResults(includeArray, search));
            }

            catch
            {
                return View("SearchResults", new List<Search>());
            }
        }

        [Route("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private List<Search> GetSearchResults(bool[] categories, string searchTerm)
        {
            List<Search> searchResults = new List<Search>();
            Errors error = Errors.Null;

            var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
            if (error != Errors.Null)
                throw new InvalidOperationException("The database was unable to access the Infocards collection.");

            if (categories[0])
            {
                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var marketEquipment = Utils.GetDatabaseCollection<Market>("MarketsEquipment", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsEquipment collection.");

                var marketShips = Utils.GetDatabaseCollection<Market>("MarketsShips", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsShips collection.");

                var marketCommodities = Utils.GetDatabaseCollection<Market>("MarketsCommodities", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsCommodities collection.");

                UpdateSearchResults(ref searchResults, bases, infocards, searchTerm);
                UpdateSearchResults(ref searchResults, marketCommodities, infocards, searchTerm);
                UpdateSearchResults(ref searchResults, marketEquipment, infocards, searchTerm);
                UpdateSearchResults(ref searchResults, marketShips, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[1])
            {
                var commodities = Utils.GetDatabaseCollection<Commodity>("Commodities", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException(
                        "The database was unable to access the Commodities collection.");

                UpdateSearchResults(ref searchResults, commodities, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[2])
            {
                var cms = Utils.GetDatabaseCollection<CountermeasureDropper>("CMs", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException(
                        "The database was unable to access the CMs collection.");

                UpdateSearchResults(ref searchResults, cms, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[3])
            {
                var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
                    if (error != Errors.Null)
                        throw new InvalidOperationException("The database was unable to access the Factions collection.");

                    UpdateSearchResults(ref searchResults, factions, infocards, searchTerm);
                    if (searchResults.Count >= _maxSearchResults)
                        return searchResults;
            }

            if (categories[4])
            {
                var shields = Utils.GetDatabaseCollection<Shield>("Shields", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Shields collection.");

                UpdateSearchResults(ref searchResults, shields, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[5])
            {
                var ships = Utils.GetDatabaseCollection<Ship>("Ships", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Ships collection.");

                UpdateSearchResults(ref searchResults, ships, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[6])
            {
                var cloaks = Utils.GetDatabaseCollection<CloakingDevice>("Cloaks", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Cloaks collection.");

                var disrupters = Utils.GetDatabaseCollection<CloakDisrupter>("Disrupters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Disrupters collection.");

                UpdateSearchResults(ref searchResults, cloaks, infocards, searchTerm);
                UpdateSearchResults(ref searchResults, disrupters, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[7])
            {
                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                UpdateSearchResults(ref searchResults, systems, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[8])
            {
                var thrusters = Utils.GetDatabaseCollection<Thruster>("Thrusters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Thrusters collection.");

                UpdateSearchResults(ref searchResults, thrusters, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            if (categories[9])
            {
                var weapons = Utils.GetDatabaseCollection<Weapon>("Weapons", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Commodities collection.");

                UpdateSearchResults(ref searchResults, weapons, infocards, searchTerm);
                if (searchResults.Count >= _maxSearchResults)
                    return searchResults;
            }

            return searchResults;
        }

        private void UpdateSearchResults<T>(ref List<Search> searchResults, List<T> table, List<Infocard> infocards, string searchTerm)
        {
            foreach (var t in table)
            {
                Search search = Models.Search.CreateInstance(t, infocards);
                if (search == null) continue;
                if (search.Name.Contains(searchTerm) || search.Infocard.Contains(searchTerm) && searchResults.Count < _maxSearchResults)
                    searchResults.Add(search);
            }
        }
    }
}
