using System;
using System.Collections.Generic;
using System.Linq;
using DSCore.Ini;
using DSCore.Models;
using Microsoft.AspNetCore.Mvc;
using DSCore.Utilities;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class CommodityController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var commodities = Utils.GetDatabaseCollection<Commodity>("Commodities", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Commodities collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                List<Commodity> commoditiesList = new List<Commodity>();
                foreach (var i in commodities)
                {
                    Good good = goods.FirstOrDefault(x => x.Nickname == i.Nickname);
                    if (good == null)
                        continue;

                    Commodity commodity = i;
                    i.Price = good.Price;
                    i.BadBuyPrice = good.BadSellPrice;
                    i.Combinable = good.Combinable;
                    i.GoodSellPrice = good.GoodSellPrice;
                    i.BadSellPrice = good.BadSellPrice;
                    i.GoodBuyPrice = good.GoodBuyPrice;
                    commoditiesList.Add(commodity);
                }

                ViewBag.Infocards = infocards;
                return View(commoditiesList);
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
                var commodities = Utils.GetDatabaseCollection<Commodity>("Commodities", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Commodities collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                Commodity commodity = commodities.Find(x => x.Nickname == nickname);
                Good good = goods.Find(x => x.Nickname == nickname);
                commodity.Price = good.Price;
                commodity.BadBuyPrice = good.BadSellPrice;
                commodity.Combinable = good.Combinable;
                commodity.GoodSellPrice = good.GoodSellPrice;
                commodity.BadSellPrice = good.BadSellPrice;
                commodity.GoodBuyPrice = good.GoodBuyPrice;

                ViewBag.Infocard = new KeyValuePair<string, string>(infocards.First(x => x.Key == commodity.Name).Value, Utils.XmlToHtml(infocards.First(x => x.Key == commodity.Infocard).Value));
                return View(commodity);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
