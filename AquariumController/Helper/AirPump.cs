using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Device.Gpio;
using System.Linq;
using System.Timers;

namespace AquariumController.Helper
{
    public static class AirPump
    {
        static bool _AirPumpOnOff = false;

        static bool _TimerAirPumpOn = false;

        static DateTime _AirPumpStart;
        static DateTime _AirPumpStop;

        static List<DateTime> _FeedingTimes;

        static Timer _TimerOff = null;
        static Timer _TimerOn = null;

        static bool SetupAirPumpStartStopTimeFirstRun = true;

        public static void SetupAirPumpStartStopTime(MySqlConnection conn)
        {
            string timeStart = DB.Helper.GetSettingFromDb(conn, "AirPumpStart");
            string timeStop = DB.Helper.GetSettingFromDb(conn, "AirPumpStop");

            if (!string.IsNullOrWhiteSpace(timeStart))
            {
                _AirPumpStart = DateTime.Parse(timeStart);

                if (DateTime.Now.TimeOfDay < _AirPumpStart.TimeOfDay)
                {
                    _TimerOff = new Timer((DateTime.Now.TimeOfDay - _AirPumpStart.TimeOfDay).TotalMilliseconds);
                    _TimerOff.Elapsed += TimerOn_Elapsed;
                    _TimerOff.Start();
                }
                

                ConsoleEx.WriteLineWithDate($"AirPumpStart is {_AirPumpStart.TimeOfDay}");
            }

            if (!string.IsNullOrWhiteSpace(timeStop))
            {
                _AirPumpStop = DateTime.Parse(timeStop);

                if (DateTime.Now.TimeOfDay < _AirPumpStop.TimeOfDay)
                {
                    _TimerOff = new Timer((DateTime.Now.TimeOfDay - _AirPumpStop.TimeOfDay).TotalMilliseconds);
                    _TimerOff.Elapsed += TimerOff_Elapsed;
                    _TimerOff.Start();
                }

                ConsoleEx.WriteLineWithDate($"AirPumpStop is {_AirPumpStop.TimeOfDay}");
            }

            if(SetupAirPumpStartStopTimeFirstRun)
            {
              
                if(DateTime.Now.TimeOfDay> _AirPumpStop.TimeOfDay)
                {
                    _TimerAirPumpOn = false;
                    DB.Helper.SaveSettingValue(conn, "airPumpOnOff", false.ToString());
                }
                else
                {
                    _TimerAirPumpOn = true;
                    DB.Helper.SaveSettingValue(conn, "airPumpOnOff", true.ToString());

                }

                SetupAirPumpStartStopTimeFirstRun = false;
            }

        }

        private static void TimerOn_Elapsed(object sender, ElapsedEventArgs e)
        {
            var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            localConn.Open();

            _TimerAirPumpOn = true;
            DB.Helper.SaveSettingValue(localConn, "airPumpOnOff", true.ToString());

            localConn.Close();
            localConn.Dispose();

            _TimerOn.Stop();
        }


        private static void TimerOff_Elapsed(object sender, ElapsedEventArgs e)
        {
            var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            localConn.Open();

            _TimerAirPumpOn = false;
            DB.Helper.SaveSettingValue(localConn, "airPumpOnOff", false.ToString());

            Console.WriteLine("4");

            localConn.Close();
            localConn.Dispose();

            _TimerOff.Stop();

        }


        public static void SetupAirPumpFeedingStop(MySqlConnection conn)
        {
            string feedingTimes = DB.Helper.GetSettingFromDb(conn, "FeedingTimes");

            string[] feedigTimes = feedingTimes.Split('#');

            _FeedingTimes = new List<DateTime>();

            foreach (string feedingTime in feedigTimes)
            { 
                _FeedingTimes.Add(DateTime.Parse(feedingTime));
                ConsoleEx.WriteLineWithDate($"Add feeding time {feedingTime}");
            }
        }


        public static void SetAirPumpFeedingOff(MySqlConnection conn)
        {
            //if _TimerAirPumpOnOff is off, then do nothing
            if (_TimerAirPumpOn)
            {
                if (_FeedingTimes != null)
                {
                    //turn off air pump if now is 1 min before any feeding times and 3 med after any feeding times
                    if (_FeedingTimes.Any(t => t.AddMinutes(-1).TimeOfDay <= DateTime.Now.TimeOfDay && DateTime.Now.TimeOfDay < t.AddMinutes(3).TimeOfDay))
                    {
                        DB.Helper.SaveSettingValue(conn, "airPumpOnOff", false.ToString());
                    }
                    else
                    {
                        //if _TimerAirPumpOnOff is off, then do not turn the air pump back on
                        if (!_TimerAirPumpOn)
                        {
                            DB.Helper.SaveSettingValue(conn, "airPumpOnOff", true.ToString());

                        }

                    }
                }
            }
        }

        public static void AirPumpOnOff(MySqlConnection conn, GpioController gpioController, int airPumpPin )
        {

            if (bool.TryParse(DB.Helper.GetSettingFromDb(conn, "AirPumpOnOff"), out bool result))
            {
                if (_AirPumpOnOff != result)
                {
                    _AirPumpOnOff = result;

                    if (result)
                    {

                        gpioController.Write(airPumpPin, PinValue.High);
                        ConsoleEx.WriteLineWithDate($"Air pump on!");
                    }
                    else
                    {
                        gpioController.Write(airPumpPin, PinValue.Low);

                        ConsoleEx.WriteLineWithDate($"Air pump off!");
                    }
                }

            }

        }







    }
}
