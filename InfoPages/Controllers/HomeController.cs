using InfoPages.Models;
using Iot.Device.CpuTemperature;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;

namespace InfoPages.Controllers
{
    public class HomeController : Controller
    {
        CpuTemperature cpuTemperature = new CpuTemperature();

        public ActionResult Index()
        {
            return View(ReadTemperatureValues());
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

            MySqlDataReader rdr = cmd.ExecuteReader();

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

            MySqlDataReader rdr = cmd.ExecuteReader();
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

            int rdr = cmd.ExecuteNonQuery();

            return RedirectToAction("Settings");
        }

        public ActionResult Temperature()
        {

            return View(ReadTemperatureValues());
        }

        public ActionResult PH()
        {

            return View(new Graph() { TimeSpan = EnumTimeSpan.OneMonth });
        }

        public ActionResult TemperatureData(EnumTimeSpan? timeSpan)
        {

            JsonSerializerSettings _jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            return Content(JsonConvert.SerializeObject(GetData(timeSpan, "temperature"), _jsonSetting), "application/json");

        }

        public ActionResult PHData(EnumTimeSpan? timeSpan)
        {

            JsonSerializerSettings _jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            return Content(JsonConvert.SerializeObject(GetData(timeSpan, "ph"), _jsonSetting), "application/json");

        }



        public ActionResult CPUTemperature()
        {
            double temperature = 0;
            if (cpuTemperature.IsAvailable)
            {
                temperature = cpuTemperature.Temperature.DegreesCelsius;
            }
            JsonSerializerSettings _jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            return Content(JsonConvert.SerializeObject(Math.Round(temperature, 1), _jsonSetting), "application/json");
        }

        private static MySqlConnection OpenConnection()
        {
            MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["mysql"].ConnectionString);
            conn.Open();
            return conn;
        }

        private static Graph ReadTemperatureValues()
        {
            int _temperatureMax = 0;
            int _temperatureMin = 0;

            MySqlConnection conn = OpenConnection();

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT value FROM settings where title = 'TemperatureMax'",
                Connection = conn
            };
            MySqlDataReader rdr = cmd.ExecuteReader();

            rdr.Read();
            _temperatureMax = int.Parse(rdr["value"].ToString());

            rdr.Close();
            cmd = new MySqlCommand
            {
                CommandText = "SELECT value FROM settings where title = 'TemperatureMin'",
                Connection = conn
            };
            rdr = cmd.ExecuteReader();

            rdr.Read();
            _temperatureMin = int.Parse(rdr["value"].ToString());


            rdr.Close();
            conn.Close();

            return new Graph() { Max = _temperatureMax, Min = _temperatureMin, TimeSpan = EnumTimeSpan.OneMonth };
        }

        private static string GetWhereFromTimeSpan(EnumTimeSpan? timeSpan)
        {
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

            return where;
        }

        private static List<DataPoint> GetData(EnumTimeSpan? timeSpan, string tableName)
        {
            List<DataPoint> dataPoints = new List<DataPoint>();

            MySqlConnection conn = OpenConnection();
            string where = GetWhereFromTimeSpan(timeSpan);

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT value, created_at FROM " + tableName + where,
                Connection = conn
            };

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                dataPoints.Add(new DataPoint(DateTime.Parse(rdr["created_at"].ToString()), double.Parse(rdr["value"].ToString())));
            }

            rdr.Close();
            conn.Close();

            return dataPoints;
        }
    }
}
