using System;
using System.Collections.Generic;
using System.Linq;
using DSCore.Ini;
using Microsoft.AspNetCore.Mvc;
using DSCore.Models;
using DSCore.Utilities;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class ShipController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var ships = Utils.GetDatabaseCollection<Ship>("Ships", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Ships collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                List<Ship> shipsList = new List<Ship>();
                foreach (var i in ships)
                {
                    Good good = goods.FirstOrDefault(x => x.Nickname.Replace("_package", "") == i.Nickname);
                    if (good == null)
                        continue;

                    Ship ship = i;
                    ship.Price = good.Price;
                    shipsList.Add(ship);
                }

                ViewBag.Infocards = infocards;
                return View(shipsList);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }

        [HttpGet("{nickname}")]
        public IActionResult Individual(string nickname)
        {
            Errors error = Errors.Null;
            try
            {
                var ships = Utils.GetDatabaseCollection<Ship>("Ships", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Ships collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                var marketShips = Utils.GetDatabaseCollection<Market>("MarketsShips", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsShips collection.");

                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                Dictionary<string, decimal> baseList = new Dictionary<string, decimal>();
                foreach (var i in marketShips)
                { 
                    if (i.Good.ContainsKey(nickname))
                        baseList.Add(i.Base, i.Good.FirstOrDefault(x => x.Key == nickname).Value);
                }

                Dictionary<Base, decimal> sellpoints = new Dictionary<Base, decimal>();
                foreach (var s in baseList)
                    sellpoints[bases.First(x => x.Nickname == s.Key)] = s.Value;

                Ship ship = ships.Find(x => x.Nickname == nickname);
                Good good = goods.Find(x => x.Nickname.Replace("_package", "") == nickname);
                ship.Price = good.Price;

                ViewBag.Infocards = infocards;
                ViewBag.Sellpoints = sellpoints;
                ViewBag.Systems = systems;
                return View(ship);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
