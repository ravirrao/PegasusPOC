using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcMovie.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
           Trace.TraceWarning("Index method: " + DateTime.Now.Ticks.ToString());
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            Trace.TraceInformation("About method: " + DateTime.Now.Ticks.ToString());
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            Trace.TraceError("Contact method: " + DateTime.Now.Ticks.ToString());
            return View();
        }
    }
}