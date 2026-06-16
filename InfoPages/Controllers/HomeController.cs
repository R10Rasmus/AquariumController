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
            return View(new DashboardViewModel
            {
                Graph = ReadTemperatureValues(),
                PumperStateChanges = GetPumperStateChanges(5)
            });
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

        public ActionResult Pumper()
        {
            return View(GetPumperStateChanges(100));
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

        public ActionResult CoolerEventsData(EnumTimeSpan? timeSpan)
        {
            List<object> events = new List<object>();

            MySqlConnection conn = OpenConnection();
            string where = GetWhereFromTimeSpan(timeSpan);

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT channel, state, created_at FROM cooler_state_changes" + where + " ORDER BY created_at ASC",
                Connection = conn
            };

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                events.Add(new
                {
                    Channel = rdr["channel"].ToString(),
                    State = Convert.ToInt32(rdr["state"]) == 1,
                    CreatedAt = DateTime.Parse(rdr["created_at"].ToString())
                });
            }

            rdr.Close();
            conn.Close();

            JsonSerializerSettings _jsonSetting = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            return Content(JsonConvert.SerializeObject(events, _jsonSetting), "application/json");
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

        private static List<PumperStateChange> GetPumperStateChanges(int count)
        {
            List<PumperStateChange> changes = new List<PumperStateChange>();

            MySqlConnection conn = OpenConnection();

            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = "SELECT from_state, to_state, created_at FROM pumper_state_changes ORDER BY created_at DESC LIMIT " + count,
                Connection = conn
            };

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                changes.Add(new PumperStateChange
                {
                    Timestamp = DateTime.Parse(rdr["created_at"].ToString()),
                    FromState = Convert.ToInt32(rdr["from_state"]) == 1,
                    ToState = Convert.ToInt32(rdr["to_state"]) == 1
                });
            }

            rdr.Close();
            conn.Close();

            return changes;
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
