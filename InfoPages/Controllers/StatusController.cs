using System;
using System.Linq;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using InfoPages.Models;
using System.Configuration;

namespace InfoPages.Controllers
{
    public class StatusController : Controller
    {
        private MySqlConnection OpenConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;
            return new MySqlConnection(connectionString);
        }

        public ActionResult GetStatus()
        {
            StatusModel status = new StatusModel();

            using (MySqlConnection conn = OpenConnection())
            {
                conn.Open();

                string query = "SELECT title, value FROM settings WHERE title IN ('FanCoolerOnOff', 'ExtraCoolerOnOff')"; // Replace 'statusTable' with your actual table name

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr["title"].ToString() == "FanCoolerOnOff")
                        {
                            status.FanCoolerOnOff = rdr["value"].ToString();
                        }
                        else if (rdr["title"].ToString() == "ExtraCoolerOnOff")
                        {
                            status.ExtraCoolerOnOff = rdr["value"].ToString();
                        }
                    }
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }
    }

    public class StatusModel
    {
        public string FanCoolerOnOff { get; set; }
        public string ExtraCoolerOnOff { get; set; }
    }
}
