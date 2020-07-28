using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquariumController.Helper
{
    public static class AirPump
    {
        static bool _AirPumpOnOff = false;

        static bool? _TimerAirPumpOnOff = null;


        static DateTime _AirPumpStart;
        static DateTime _AirPumpStop;

        public static void SetupAirPumpStartStopTime(MySqlConnection conn)
        {
            string timeStart = DB.Helper.GetSettingFromDb(conn, "AirPumpStart");
            string timeStop = DB.Helper.GetSettingFromDb(conn, "AirPumpStop");

            if (!string.IsNullOrWhiteSpace(timeStart))
            {
                _AirPumpStart = DateTime.Parse(timeStart);

                Console.WriteLine($"AirPumpStart is {_AirPumpStart.TimeOfDay}");
            }

            if (!string.IsNullOrWhiteSpace(timeStop))
            {
                _AirPumpStop = DateTime.Parse(timeStop);

                Console.WriteLine($"AirPumpStop is {_AirPumpStop.TimeOfDay}");
            }
        }

        //fixme make this a time runnig only running when it needs to, on the time of _AirPumpStart or _AirPumpStop
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
                        Console.WriteLine($"Air pump on!");
                    }
                    else
                    {
                        gpioController.Write(airPumpPin, PinValue.Low);

                        Console.WriteLine($"Air pump off!");
                    }
                }

            }

        }

    }
}
