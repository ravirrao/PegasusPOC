//#define NotTestingTimeOut
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cache;
using MvcMovie.Models;
using System.Diagnostics;
using StackExchange.Redis;
using System.Web.SessionState;

namespace MvcMovie.Controllers
{
   [SessionState(SessionStateBehavior.Disabled)]
   // Disable session state when stress testing to avoid session state timeout exceptions
   public class MoviesController : Controller
   {
      private MovieDBContext db = new MovieDBContext();
      private static ConnectionMultiplexer connection;
      private static ConnectionMultiplexer connectionLong;

#if NotTestingTimeOut
   private static ConnectionMultiplexer Connection
   {
      get
      {
         if (connection == null || !connection.IsConnected)
         {
            connection = ConnectionMultiplexer.Connect(Keys.conStr);
         }
         return connection;
      }
   }
#else
      #region StressTest
      private static ConnectionMultiplexer Connection
      {

         get
         {
            if (connection != null && connection.IsConnected)
            {
               return connection;
            }
            var config = new ConfigurationOptions();
            config.EndPoints.Add(Keys.URL);
            config.Password = Keys.passwd;
            config.Ssl = true;
            config.SyncTimeout = 150;

            connection = ConnectionMultiplexer.Connect(config);
            return connection;
         }
      }
      #endregion
#endif
      private static ConnectionMultiplexer ConnectionLong
      {
         get
         {
            if (connectionLong != null && connectionLong.IsConnected)
            {
               return connectionLong;
            }
            var config = new ConfigurationOptions();
            config.EndPoints.Add(Keys.URL);
            config.Password = Keys.passwd;
            config.Ssl = true;
            // 30 second timeout too high for a UI, used for testing.
            config.SyncTimeout = 30 * 1000;

            connectionLong = ConnectionMultiplexer.Connect(config);
            return connectionLong;
         }
      }
      // GET: /Movies/

      // GET: /Movies/
      public ActionResult Index(string movieGenre, string searchString)
      {
         var GenreLst = new List<string>();

         var GenreQry = from d in db.Movies
                        orderby d.Genre
                        select d.Genre;

         GenreLst.AddRange(GenreQry.Distinct());
         ViewBag.movieGenre = new SelectList(GenreLst);

         var movies = from m in db.Movies
                      select m;

         if (!String.IsNullOrEmpty(searchString))
         {
            movies = movies.Where(s => s.Title.Contains(searchString));
         }

         if (!string.IsNullOrEmpty(movieGenre))
         {
            movies = movies.Where(x => x.Genre == movieGenre);
         }

         return View(movies);
      }

      Movie getMovie(int id, int retryAttempts = 0)
      {
         IDatabase cache = Connection.GetDatabase();
         if (retryAttempts > 3)
         {
            string error = "getMovie timeout with " + retryAttempts.ToString()
               + " retry attempts. Movie ID = " + id.ToString();
            Logger(error);

            ViewBag.cacheMsg = error + " Fetch from DB";
            // Cache unavailable, get data from DB
            return db.Movies.Find(id);
         }
         Stopwatch sw = Stopwatch.StartNew();
         Movie m;

         try
         {
            m = (Movie)cache.Get(id.ToString());
         }

         catch (TimeoutException tx)
         {
            Logger("getMovie fail, ID = " + id.ToString(), tx);
            return getMovie(id, ++retryAttempts);
         }

         if (m == null)
         {
            Movie movie = db.Movies.Find(id);
            cache.Set(id.ToString(), movie);
            StopWatchMiss(sw);
            return movie;
         }
         StopWatchHit(sw);

         return m;
      }

      private void Logger(string message, TimeoutException tx = null)
      {
         Trace.TraceWarning(message);
         if (tx != null)
         {
            Trace.TraceWarning(tx.Message);
         }
         // Log the timeout exception.
      }

      private void ClearMovieCache(int p)
      {
         IDatabase cache = connection.GetDatabase();
         if (cache.KeyExists(p.ToString()))
            cache.KeyDelete(p.ToString());
      }

      void StopWatchEnd(Stopwatch sw, string msg = "")
      {
         sw.Stop();
         double ms = sw.ElapsedTicks / (Stopwatch.Frequency / (1000.0));
         string tmMsg = "MS: " + ms.ToString();
         if (ms > 3000)
         {
            tmMsg = "Seconds: " + (ms / 1000).ToString();
         }
         ViewBag.cacheMsg = msg + tmMsg +
             " PID: " + Process.GetCurrentProcess().Id.ToString();
      }

      void StopWatchMiss(Stopwatch sw)
      {
         StopWatchEnd(sw, "Miss ");
      }

      void StopWatchHit(Stopwatch sw)
      {
         StopWatchEnd(sw, "Hit ");
      }

      public ActionResult CacheBig(int id = 1)
      {
         int[] array1 = new int[1000 * 1000 * id];

         IDatabase cache = ConnectionLong.GetDatabase();
         Stopwatch sw = Stopwatch.StartNew();

         string key = "Big" + DateTime.Now.Ticks.ToString();

         try
         {
            cache.Set("key", array1);
            cache.KeyExpire("key", TimeSpan.FromMinutes(3));
         }
         catch (System.TimeoutException stoe)
         {
            ViewBag.errorMsg = stoe.Message;
            StopWatchEnd(sw);
            return View("Error");
         }

         ViewBag.InfoMsg = (4 * id).ToString() + " MB blob added to cache";
         StopWatchEnd(sw);

         return View("Info");
      }

      public ActionResult WriteCache(int id = 1)
      {
         Stopwatch sw = Stopwatch.StartNew();
         IDatabase cache = ConnectionLong.GetDatabase();
         int cntMax = 1000 * id;

         try
         {
            for (int i = 0; i < cntMax; i++)
            {
               cache.StringSet("c" + i.ToString(), i * 3);
            }
         }
         catch (TimeoutException tx)
         {
            ViewBag.errorMsg = tx.Message + " attempting to add" + id.ToString() + " K items added to cache";
            return View("Error");
         }
         StopWatchEnd(sw);
         ViewBag.InfoMsg = (id.ToString() + "K items added to cache");
         return View("Info");
      }
      public ActionResult ReadCache(int id = 1)
      {
         Stopwatch sw = Stopwatch.StartNew();
         IDatabase cache = ConnectionLong.GetDatabase();
         int cntMax = 1000 * id;

         for (int i = 0; i < cntMax; i++)
         {
            string item = cache.StringGet("c" + i.ToString());
            if (Convert.ToInt32(item) != i * 3)
            {
               ViewBag.errorMsg = "Invalid cache data, i=" + i.ToString() + " Val= " + item.ToString()
                  + "  Should be: " + (i * 3).ToString() + " size = " + id.ToString() + " K";
               return View("Error");
            }
         }

         StopWatchEnd(sw);
         ViewBag.InfoMsg = id.ToString() + " K items read from the cache";
         return View("Info");
      }


      public ActionResult Ping()
      {
         IDatabase cache = Connection.GetDatabase();
         Stopwatch sw = Stopwatch.StartNew();

         TimeSpan ts = cache.Ping();

         ViewBag.InfoMsg = "Ping (latency) = " + ts.Milliseconds + " MS";
         return View("Info");
      }

      public ActionResult Details(int? id)
      {
         if (id == null)
         {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
         }
         //Movie movie = db.Movies.Find(id);
         Movie movie = getMovie((int)id);
         if (movie == null)
         {
            return HttpNotFound();
         }
         return View(movie);
      }

      // GET: /Movies/Create
      public ActionResult Create()
      {
         return View(new Movie
         {
            Genre = "Comedy",
            Price = 3.99M,
            ReleaseDate = DateTime.Now,
            Rating = "G",
            Title = "Ghotst Busters III"
         });
      }
      /*
public ActionResult Create()
{
  return View();
}

*/
      // POST: /Movies/Create
      // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
      // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
      [HttpPost]
      [ValidateAntiForgeryToken]
      public ActionResult Create([Bind(Include = "ID,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
      {
         if (ModelState.IsValid)
         {
            db.Movies.Add(movie);
            db.SaveChanges();
            return RedirectToAction("Index");
         }

         return View(movie);
      }

      public ActionResult ClearCache()
      {
         IDatabase cache = Connection.GetDatabase();

         var movies = from m in db.Movies
                      select m;

         try
         {
            foreach (Movie mv in movies)
            {
               cache.KeyDelete(mv.ID.ToString());
            }
         }
         catch (System.TimeoutException stoe)
         {
            ViewBag.errorMsg = stoe.Message + " From ClearCache";
            return View("Error");
         }
         return RedirectToAction("Index");

      }
      // GET: /Movies/Edit/5
      public ActionResult Edit(int? id)
      {
         if (id == null)
         {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
         }
         //Movie movie = db.Movies.Find(id);
         Movie movie = getMovie((int)id);
         if (movie == null)
         {
            return HttpNotFound();
         }
         return View(movie);
      }

      // POST: /Movies/Edit/5
      // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
      // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
      [HttpPost]
      [ValidateAntiForgeryToken]
      public ActionResult Edit([Bind(Include = "ID,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
      {
         if (ModelState.IsValid)
         {
            db.Entry(movie).State = EntityState.Modified;
            db.SaveChanges();
            ClearMovieCache(movie.ID);
            return RedirectToAction("Index");
         }
         return View(movie);
      }

      public ActionResult WriteAzureCache(int id = 1)
      {
          Stopwatch sw = Stopwatch.StartNew();

          int cntMax = 1000 * id;

          try
          {
              for (int i = 0; i < cntMax; i++)
              {
                  CacheHelper.SetCachedData("c" + i.ToString(), (i * 3).ToString());
              }
          }
          catch (TimeoutException tx)
          {
              ViewBag.errorMsg = tx.Message + " attempting to add" + id.ToString() + " K items added to cache";
              return View("Error");
          }
          StopWatchEnd(sw);
          ViewBag.InfoMsg = ("1000 items added to azure cache");
          return View("Info");
      }

      public ActionResult ReadAzureCache(int id = 1)
      {
          Stopwatch sw = Stopwatch.StartNew();

          int cntMax = 1000 * id;

          for (int i = 0; i < cntMax; i++)
          {
              string item = CacheHelper.GetCachedData<string>("c" + i.ToString());
              if (Convert.ToInt32(item) != i * 3)
              {
                  ViewBag.errorMsg = "Invalid cache data, i=" + i.ToString() + " Val= " + item
                                     + "  Should be: " + (i * 3).ToString() + " size = " + id.ToString() + " K";
                  return View("Error");
              }
          }

          StopWatchEnd(sw);
          ViewBag.InfoMsg = "1000 items read from the azure cache";
          return View("Info");
      }

      public ActionResult WriteManagedCache(int id = 1)
      {
          Stopwatch sw = Stopwatch.StartNew();

          int cntMax = 1000 * id;

          try
          {
              for (int i = 0; i < cntMax; i++)
              {
                  CacheHelper.SetCachedData("c" + i.ToString(), (i * 3).ToString(), true);
              }
          }
          catch (TimeoutException tx)
          {
              ViewBag.errorMsg = tx.Message + " attempting to add" + id.ToString() + " K items added to cache";
              return View("Error");
          }
          StopWatchEnd(sw);
          ViewBag.InfoMsg = ("1000 items added to azure managed cache");
          return View("Info");
      }

      public ActionResult ReadManagedCache(int id = 1)
      {
          Stopwatch sw = Stopwatch.StartNew();

          int cntMax = 1000 * id;

          for (int i = 0; i < cntMax; i++)
          {
              string item = CacheHelper.GetCachedData<string>("c" + i.ToString(), true);
              if (Convert.ToInt32(item) != i * 3)
              {
                  ViewBag.errorMsg = "Invalid cache data, i=" + i.ToString() + " Val= " + item
                                     + "  Should be: " + (i * 3).ToString() + " size = " + id.ToString() + " K";
                  return View("Error");
              }
          }

          StopWatchEnd(sw);
          ViewBag.InfoMsg = "1000 items read from the azure managed cache";
          return View("Info");
      }

      // GET: /Movies/Delete/5
      public ActionResult Delete(int? id)
      {
         if (id == null)
         {
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
         }
         //Movie movie = db.Movies.Find(id);
         Movie movie = getMovie((int)id);
         if (movie == null)
         {
            return HttpNotFound();
         }
         return View(movie);
      }

      // POST: /Movies/Delete/5
      [HttpPost, ActionName("Delete")]
      [ValidateAntiForgeryToken]
      public ActionResult DeleteConfirmed(int id)
      {
         //Movie movie = db.Movies.Find(id);
         Movie movie = getMovie((int)id);
         db.Movies.Remove(movie);
         db.SaveChanges();
         ClearMovieCache(movie.ID);
         return RedirectToAction("Index");
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            db.Dispose();
         }
         base.Dispose(disposing);
      }
   }
}
