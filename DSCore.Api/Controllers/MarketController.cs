using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiteDB;
using DSCore.Ini;

namespace DSCore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            return RedirectToPage(Request.Path + @"/commodity");
        }
    }


    [Route("api/market/commodity")]
    [ApiController]
    public class MarketCommodityController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Market>("MarketsCommodities");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                return Utils.ReturnJson(collection.FindAll(), Errors.Null);
            }
        }

        [HttpGet("{nickname}")]
        public ActionResult<string> Get(string nickname)
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Market>("MarketsCommodities");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                var find = collection.Find(x => x.Good.ContainsKey(nickname));
                if (find == null)
                    return Utils.ReturnJson(null, Errors.ValueNotFound);
                else
                {
                    return Utils.ReturnJson(find, Errors.Null);
                }
            }
        }
    }

    [Route("api/market/equipment")]
    [ApiController]
    public class MarketEquipmentController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Market>("MarketsEquipment");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                return Utils.ReturnJson(collection.FindAll(), Errors.Null);
            }
        }

        [HttpGet("{nickname}")]
        public ActionResult<string> Get(string nickname)
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Market>("MarketsEquipment");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                var find = collection.Find(x => x.Good.ContainsKey(nickname));
                if (find == null)
                    return Utils.ReturnJson(null, Errors.ValueNotFound);
                else
                {
                    return Utils.ReturnJson(find, Errors.Null);
                }
            }
        }
    }
}