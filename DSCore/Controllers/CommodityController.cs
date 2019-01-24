using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSCore.Api;
using DSCore.Ini;
using Microsoft.AspNetCore.Mvc;
using DSCore.Models;
using DSCore.Utilities;
using Newtonsoft.Json;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class CommodityController : InheritController
    {
        public IActionResult Index()
        {

            List<Commodity> commodities = Utils.GetAPIResponse<Commodity[]>(Utils.Endpoints.commodity.ToString()).ToList();
            Good[] goods = Utils.GetAPIResponse<Good[]>(Utils.Endpoints.good.ToString());
            Infocard[] infocards = Utils.GetAPIResponse<Infocard[]>(Utils.Endpoints.infocard.ToString());
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
                if (info == null)
                    continue;
                infocardPairs[info.Key] = info.Value;
                commodities[index] = i;
            }

            ViewBag.Infocards = infocardPairs;
            return View(commodities);

        }

        [HttpGet("{nickname}")]
        public IActionResult Index(string nickname)
        {
            Commodity commodity = Utils.GetAPIResponse<Commodity>(Utils.Endpoints.commodity + "/" + nickname);
            Good good = Utils.GetAPIResponse<Good>(Utils.Endpoints.good + "/" + nickname);
            string i = Utils.GetAPIResponse<Infocard>(Utils.Endpoints.infocard + "/" + commodity.Name).Value;
            string ii = Utils.GetAPIResponse<Infocard>(Utils.Endpoints.infocard + "/" + commodity.Infocard).Value;
            ii = Utils.XmlToHtml(ii);
            KeyValuePair<string, string> infocard = new KeyValuePair<string, string>(i, ii);

            commodity.Price = good.Price;
            commodity.BadBuyPrice = good.BadSellPrice;
            commodity.Combinable = good.Combinable;
            commodity.GoodSellPrice = good.GoodSellPrice;
            commodity.BadSellPrice = good.BadSellPrice;
            commodity.GoodBuyPrice = good.GoodBuyPrice;

            ViewBag.Infocard = infocard;
            return View("Individual", commodity);
        }
    }
}
