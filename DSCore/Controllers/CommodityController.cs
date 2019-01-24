using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DSCore.Api;
using DSCore.Ini;
using Microsoft.AspNetCore.Mvc;
using DSCore.Models;
using DSCore.Utils;
using Newtonsoft.Json;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class CommodityController : InheritController
    {
        public IActionResult Index()
        {

            List<Commodity> commodities = API.GetAPIResponse<Commodity[]>(API.Endpoints.commodity.ToString()).ToList();
            Good[] goods = API.GetAPIResponse<Good[]>(API.Endpoints.good.ToString());
            Infocard[] infocards = API.GetAPIResponse<Infocard[]>(API.Endpoints.infocard.ToString());
            Dictionary<uint, string> infocardPairs = new Dictionary<uint, string>();

            for (var index = 0; index < commodities.Count; index++)
            {
                Commodity i = commodities[index];
                Good good = Array.Find(goods, g => g.Equipment == i.Nickname);
                if (good == null)
                {
                    commodities.RemoveAt(index);
                    index--;
                    continue;
                }
                i.Price = good.Price;
                i.BadBuyPrice = good.BadSellPrice;
                i.Combinable = good.Combinable;
                i.GoodSellPrice = good.GoodSellPrice;
                i.BadSellPrice = good.BadSellPrice;
                i.GoodBuyPrice = good.GoodBuyPrice;
                Infocard info = Array.Find(infocards, a => a.Key == i.Name);
                infocardPairs[info.Key] = info.Value;
                commodities[index] = i;
            }

            ViewBag.Infocards = infocardPairs;
            return View(commodities);

        }

        [HttpGet("{nickname}")]
        public IActionResult Index(string nickname)
        {
            Commodity commodity = API.GetAPIResponse<Commodity>(API.Endpoints.commodity.ToString() + "/" + nickname);
            Good good = API.GetAPIResponse<Good>(API.Endpoints.good.ToString() + "/" + nickname);
            KeyValuePair<string, string> infocard = new KeyValuePair<string, string>(
                API.GetAPIResponse<string>(API.Endpoints.infocard.ToString() + "/" + commodity.Name),
                API.GetAPIResponse<string>(API.Endpoints.infocard.ToString() + "/" + commodity.Infocard));

            commodity.Price = good.Price;
            commodity.BadBuyPrice = good.BadSellPrice;
            commodity.Combinable = good.Combinable;
            commodity.GoodSellPrice = good.GoodSellPrice;
            commodity.BadSellPrice = good.BadSellPrice;
            commodity.GoodBuyPrice = good.GoodBuyPrice;

            ViewBag.Infocard = infocard;
            return View(commodity);
        }
    }
}
