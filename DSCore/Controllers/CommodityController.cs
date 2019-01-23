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
    public class CommodityController : InheritController
    {
        public IActionResult Index()
        {
            using (var webClient = new WebClient())
            {
                string jsonString = webClient.DownloadString("https://localhost:3080/api/commodity");
                JsonWrapper jsonObject = JsonConvert.DeserializeObject<JsonWrapper>(jsonString);
                Commodity[] commodities = JsonConvert.DeserializeObject<Commodity[]>(jsonObject.Result.ToString());

                string jsonGoods = webClient.DownloadString("https://localhost:3080/api/good");
                JsonWrapper jsonGoodsObject = JsonConvert.DeserializeObject<JsonWrapper>(jsonGoods);
                Good[] goods = JsonConvert.DeserializeObject<Good[]>(jsonGoodsObject.Result.ToString());

                for (var index = 0; index < commodities.Length; index++)
                {
                    Commodity i = commodities[index];
                    Good good = Array.Find(goods, g => g.Equipment == i.Nickname);
                    if (good == null)
                        continue;
                    i.Price = good.Price;
                    i.BadBuyPrice = good.BadSellPrice;
                    i.Combinable = good.Combinable;
                    i.GoodSellPrice = good.GoodSellPrice;
                    i.BadSellPrice = good.BadSellPrice;
                    i.GoodBuyPrice = good.GoodBuyPrice;
                    commodities[index] = i;
                }

                

                return View(commodities);
            }
            
        }
    }
}
