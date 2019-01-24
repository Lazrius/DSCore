using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiteDB;
using DSCore.Ini;
using System.Reflection;
using Newtonsoft.Json;

namespace DSCore.Api.Controllers
{
    [ApiController]
    public class DefaultController : Controller
    {
        [Route("")]
        [HttpGet]
        public ActionResult<string> Index()
        {
            return Content("Hi, I'm a test string.");
        }

        [Route("api")]
        [HttpGet]
        public ActionResult Redirect()
        {
            Response.Redirect("https://localhost:3081/api");
            return new EmptyResult(); 
        }
    }
}