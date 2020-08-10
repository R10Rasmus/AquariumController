using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Device.Gpio;
using System.Globalization;
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

        static Timer _TimerOff = null;
        static Timer _TimerOn = null;

        static Timer _FeedingOn = null;
        static Timer _FeedingOff = null;

        static bool SetupAirPumpStartStopTimeFirstRun = true;

        public static void SetupAirPumpStartStopTime(MySqlConnection conn)
        {
            string timeStart = DB.Helper.GetSettingFromDb(conn, "AirPumpStart");
            string timeStop = DB.Helper.GetSettingFromDb(conn, "AirPumpStop");

            if (!string.IsNullOrWhiteSpace(timeStart))
            {
                _AirPumpStart = DateTime.Parse(timeStart);

                DateTime baseDate = DateTime.Now;

                //time is tomorrow 
                if (DateTime.Now.TimeOfDay > _AirPumpStart.TimeOfDay)
                {
                    baseDate = DateTime.Now.AddDays(1);
                }

                _AirPumpStart = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, _AirPumpStart.Hour, _AirPumpStart.Minute, _AirPumpStart.Second);

                if (DateTime.Now < _AirPumpStart)
                {
                    if (_TimerOn == null)
                    {
                        _TimerOn = new Timer();
                        _TimerOn.Elapsed += TimerOn_Elapsed;

                    }

                    _TimerOn.Interval = Math.Abs((DateTime.Now - _AirPumpStart).TotalMilliseconds);
                    _TimerOn.Start();


                }
                

                ConsoleEx.WriteLineWithDate($"AirPumpStart is {_AirPumpStart.ToString(CultureInfo.CreateSpecificCulture("da-dk"))}");
            }

            if (!string.IsNullOrWhiteSpace(timeStop))
            {
                _AirPumpStop = DateTime.Parse(timeStop);

                DateTime baseDate = DateTime.Now;

                //time is tomorrow 
                if (DateTime.Now.TimeOfDay > _AirPumpStop.TimeOfDay)
                {
                    baseDate = DateTime.Now.AddDays(1);
                }

                _AirPumpStop = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, _AirPumpStop.Hour, _AirPumpStop.Minute, _AirPumpStop.Second);


                if (DateTime.Now < _AirPumpStop)
                {
                    if (_TimerOff == null)
                    {
                        _TimerOff = new Timer();
                        _TimerOff.Elapsed += TimerOff_Elapsed;

                    }

                    _TimerOff.Interval = Math.Abs((DateTime.Now - _AirPumpStop).TotalMilliseconds);
                    _TimerOff.Start();


                }

                ConsoleEx.WriteLineWithDate($"AirPumpStop is {_AirPumpStop.ToString(CultureInfo.CreateSpecificCulture("da-dk"))}");
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

        public static void SetupAirPumpFeedingStop(MySqlConnection conn)
        {

            double feedingCountDown = 0;
            string feedingTimes = DB.Helper.GetSettingFromDb(conn, "FeedingTimes");

            string[] feedigTimes = feedingTimes.Split('#');

            foreach (string feedingTime in feedigTimes)
            {
                DateTime baseDate = DateTime.Now;
                DateTime feedingDateTime = DateTime.Parse(feedingTime);
                //time is tomorrow 
                if (DateTime.Now.TimeOfDay > feedingDateTime.TimeOfDay)
                {
                    baseDate = DateTime.Now.AddDays(1);
                }

                feedingDateTime = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, feedingDateTime.Hour, feedingDateTime.Minute, feedingDateTime.Second);


                double countDown = (DateTime.Now - feedingDateTime).TotalMilliseconds;

                if (feedingCountDown == 0 || feedingCountDown < countDown)
                {
                    feedingCountDown = countDown;

                    ConsoleEx.WriteLineWithDate($"Next feeding time {feedingDateTime.ToString(CultureInfo.CreateSpecificCulture("da-dk"))}");
                }
            }

            //feeding starter 1 min before set time
            feedingCountDown += 60000;

            if (_FeedingOff == null)
            {
                _FeedingOff = new Timer();
                _FeedingOff.Elapsed += FeedingOff_Elapsed;
            }

            _FeedingOff.Interval = Math.Abs(feedingCountDown);
            _FeedingOff.Start();



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

        private static void FeedingOn_Elapsed(object sender, ElapsedEventArgs e)
        {
            _FeedingOn.Stop();
            SetAirPumpOnOff(true);

            ConsoleEx.WriteLineWithDate("Feeding mode is off.");
        }

        private static void FeedingOff_Elapsed(object sender, ElapsedEventArgs e)
        {
            _FeedingOff.Stop();
            SetAirPumpOnOff(false);

            //stop feeding after 4 min.
            if(_FeedingOn==null)
            {
                _FeedingOn = new Timer();
                _FeedingOn.Elapsed += FeedingOn_Elapsed;
            }

             _FeedingOn.Interval = 4 * 60 * 1000;
            _FeedingOn.Start();

            ConsoleEx.WriteLineWithDate($"Feeding mode is on. It stops in {_FeedingOn.Interval/1000} sec");
        }

        private static void TimerOn_Elapsed(object sender, ElapsedEventArgs e)
        {
            _TimerOn.Stop();
            SetAirPumpOnOff(true);
        }

        private static void TimerOff_Elapsed(object sender, ElapsedEventArgs e)
        {
            _TimerOff.Stop();
            SetAirPumpOnOff(false);


        }


        private static void SetAirPumpOnOff(bool value)
        {
            var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            localConn.Open();

            _TimerAirPumpOn = value;
            DB.Helper.SaveSettingValue(localConn, "airPumpOnOff", value.ToString());

            localConn.Close();
            localConn.Dispose();
        }







    }
}
