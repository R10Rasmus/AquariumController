using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using InfoPages.Models;
using System.Configuration;

namespace InfoPages.Controllers
{
    public class FailsController : Controller
    {
        private MySqlConnection OpenConnection()
        {
            // Implement your logic to open a connection to the database
            string connectionString = ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;
            return new MySqlConnection(connectionString);
        }

        public ActionResult Index(string sortOrder, int page = 1)
        {
            int pageSize = 10;
            List<Fails> fails = new List<Fails>();

            using (MySqlConnection conn = OpenConnection())
            {
                conn.Open();

                string query = "SELECT id, message, stacktrace, created_at FROM fails";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        fails.Add(new Fails
                        {
                            Id = int.Parse(rdr["id"].ToString()),
                            Message = rdr["message"].ToString(),
                            StackTrace = rdr["stacktrace"].ToString(),
                            Created = DateTime.Parse(rdr["created_at"].ToString())
                        });
                    }
                }
            }

            bool isDescending = !String.IsNullOrEmpty(sortOrder) && sortOrder == "desc";
            fails = isDescending ? fails.OrderByDescending(f => f.Created).ToList() : fails.OrderBy(f => f.Created).ToList();

            var pagedFails = fails.Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Ceiling((double)fails.Count() / pageSize);
            ViewBag.SortOrder = isDescending ? "asc" : "desc";
            ViewBag.IsDescending = isDescending;

            return View(pagedFails);
        }

        [HttpPost]
        public ActionResult DeleteAll()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                conn.Open();

                string query = "DELETE FROM fails";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
