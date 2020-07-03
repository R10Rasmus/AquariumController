using InfoPages.Models;
using Iot.Device.CpuTemperature;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace InfoPages.Controllers
{
    public class HomeController : Controller
    {
        CpuTemperature cpuTemperature = new CpuTemperature();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Settings()
        {
            List<Setting> settings = new List<Setting>();

            MySqlConnection conn = OpenConnection();

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT id, title, value FROM settings",
                Connection = conn
            };

            var rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                settings.Add(new Setting { Id = int.Parse(rdr["id"].ToString()), Name = rdr["title"].ToString(), Value = rdr["value"].ToString() });
            }

            rdr.Close();
            conn.Close();

            return View(settings);
        }

        public ActionResult EditSetting(int Id)
        {
            MySqlConnection conn = OpenConnection();

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT id, title, value FROM settings where id = " + Id,
                Connection = conn
            };

            var rdr = cmd.ExecuteReader();
            rdr.Read();

            Setting setting = new Setting { Id = int.Parse(rdr["id"].ToString()), Name = rdr["title"].ToString(), Value = rdr["value"].ToString() };

            rdr.Close();
            conn.Close();

            return View(setting);
        }



        [HttpPost]
        public ActionResult EditSetting(int Id, string Value)
        {
            MySqlConnection conn = OpenConnection();

            //remove all after ; to remove the possibility to sql command
            Value = Value.Trim(';');

            MySqlCommand cmd = new MySqlCommand
            {

                CommandText = "Update settings set value='" + Value + "' where id=" + Id,
                Connection = conn
            };

            var rdr = cmd.ExecuteNonQuery();

            return RedirectToAction("Settings");
        }

        public ActionResult Temperature()
        {

            return View();
        }

        public ActionResult TemperatureData(EnumTimeSpan? timeSpan)
        {

            List<DataPoint> dataPoints = new List<DataPoint>();

            MySqlConnection conn = OpenConnection();

            string where = String.Empty;
            if (timeSpan.HasValue)
            {
                where = " WHERE  (created_at > '";
                switch (timeSpan.Value)
                {
                    case EnumTimeSpan.OneHour:
                        where = where + DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case EnumTimeSpan.OneDay:
                        where = where + DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case EnumTimeSpan.OneWeek:
                        where = where + DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case EnumTimeSpan.OneMonth:
                        where = where + DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case EnumTimeSpan.OneYear:
                        where = where + DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                }
                where += "')";
            }


            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT value, created_at FROM temperature" + where,
                Connection = conn
            };

            var rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                dataPoints.Add(new DataPoint(DateTime.Parse(rdr["created_at"].ToString()), double.Parse(rdr["value"].ToString())));
            }

            rdr.Close();
            conn.Close();

            JsonSerializerSettings _jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            return Content(JsonConvert.SerializeObject(dataPoints, _jsonSetting), "application/json");

        }

        public ActionResult CPUTemperature()
        {
            double temperature=0;
            if (cpuTemperature.IsAvailable)
            {
                temperature = cpuTemperature.Temperature.DegreesCelsius;
            }
            JsonSerializerSettings _jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            return Content(JsonConvert.SerializeObject(Math.Round(temperature,1), _jsonSetting), "application/json");
        }

        private static MySqlConnection OpenConnection()
        {
            MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["mysql"].ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
