using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;

namespace AquariumController.Helper
{
    public static class AirPump
    {
        static bool _AirPumpOnOff = false;

        static bool? _TimerAirPumpOnOff = null;

        static DateTime _AirPumpStart;
        static DateTime _AirPumpStop;

        static List<DateTime> _FeedingTimes;


        public static void SetupAirPumpStartStopTime(MySqlConnection conn)
        {
            string timeStart = DB.Helper.GetSettingFromDb(conn, "AirPumpStart");
            string timeStop = DB.Helper.GetSettingFromDb(conn, "AirPumpStop");

            if (!string.IsNullOrWhiteSpace(timeStart))
            {
                _AirPumpStart = DateTime.Parse(timeStart);

                ConsoleEx.WriteLineWithDate($"AirPumpStart is {_AirPumpStart.TimeOfDay}");
            }

            if (!string.IsNullOrWhiteSpace(timeStop))
            {
                _AirPumpStop = DateTime.Parse(timeStop);

                ConsoleEx.WriteLineWithDate($"AirPumpStop is {_AirPumpStop.TimeOfDay}");
            }
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


        public static void SetAirPumpOnOff(MySqlConnection conn)
        {
            if (DateTime.Now.TimeOfDay >= _AirPumpStart.TimeOfDay && DateTime.Now.TimeOfDay < _AirPumpStop.TimeOfDay)
            {
                if (_TimerAirPumpOnOff != true)
                {
                    _TimerAirPumpOnOff = true;
                    DB.Helper.SaveSettingValue(conn, "airPumpOnOff", true.ToString());
                }

            }
            else
            {

                if (_TimerAirPumpOnOff != false)
                {
                    _TimerAirPumpOnOff = false;
                    DB.Helper.SaveSettingValue(conn, "airPumpOnOff", false.ToString());
                }
            }

        }

        public static void SetAirPumpFeedingOff(MySqlConnection conn)
        {
            if (_FeedingTimes != null)
            {
                //turn off air pump if now is 1 min before any feeding times and 3 med after any feeding times
                if (_FeedingTimes.Any(t => t.AddMinutes(-1).TimeOfDay <= DateTime.Now.TimeOfDay && DateTime.Now.TimeOfDay < t.AddMinutes(3).TimeOfDay))
                {
                    _TimerAirPumpOnOff = false;
                    DB.Helper.SaveSettingValue(conn, "airPumpOnOff", false.ToString());
                }
                else
                {
                    //if _TimerAirPumpOnOff is off, then do not turn the air pump back on
                    if (!_TimerAirPumpOnOff.HasValue || !_TimerAirPumpOnOff.Value)
                    {
                        _TimerAirPumpOnOff = true;
                        DB.Helper.SaveSettingValue(conn, "airPumpOnOff", true.ToString());
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
