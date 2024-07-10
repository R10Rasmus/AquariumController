using System;
using System.Linq;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using InfoPages.Models;
using System.Configuration;
using MySqlX.XDevAPI;
using Q42.HueApi;
using System.Collections.Generic;
using Q42.HueApi.Interfaces;
using System.Web.Razor.Tokenizer;

namespace InfoPages.Controllers
{
    public class StatusController : Controller
    {
        private readonly ILocalHueClient client;
        private readonly Light aquariumFanCooler;
        private readonly Light aquariumExtraCooler;
        public StatusController()
        {
            using (MySqlConnection conn = OpenConnection())
            {
                conn.Open();
                client = new LocalHueClient(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
                client.Initialize(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

                IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

                aquariumFanCooler = lights.FirstOrDefault(t => t.Name == Helpers.DB.Helper.GetSettingFromDb(conn, "FanCoolerName"));
                aquariumExtraCooler = lights.FirstOrDefault(t => t.Name == Helpers.DB.Helper.GetSettingFromDb(conn, "ExtraCoolerName"));
            }
        }
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


                IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

                string query = "SELECT title, value FROM settings WHERE title IN ('FanCoolerrOnOff', 'ExtraCoolerOnOff')"; // Replace 'statusTable' with your actual table name

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        if (rdr["title"].ToString() == "FanCoolerrOnOff")
                        {
                            status.FanCoolerOnOff = bool.Parse( rdr["value"].ToString());
                        }
                        else if (rdr["title"].ToString() == "ExtraCoolerOnOff")
                        {
                            status.ExtraCoolerOnOff = bool.Parse(rdr["value"].ToString());
                        }
                    }
                }

                status.FanCoolerOnOffHue = aquariumFanCooler.State.On;
                if(aquariumExtraCooler != null)
                    status.ExtraCoolerOnOffHue = aquariumExtraCooler.State.On;
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }
    }

    public class StatusModel
    {
        public bool FanCoolerOnOff { get; set; }

        public bool FanCoolerOnOffHue { get; set; }
        public bool ExtraCoolerOnOff { get; set; }

        public bool ExtraCoolerOnOffHue { get; set; }
    }
}
