using Iot.Device.OneWire;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;

namespace AquariumController.Helper
{
    public static class Tempertur
    {
        const double _TemperturCalibrateOffSet = 0.6;

        public static double TemperturValue = 0;
        public static double TemperatureMin = 0;
        public static double TemperatureMax = 0;
        public static void SaveTempertur(Object stateInfo)
        {
            if (TemperturValue > 0)
            {
                Console.WriteLine($"Save tempertur with value {TemperturValue}");

                var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                DB.Helper.SaveChannelValue(localConn, "temperature", TemperturValue);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                Console.WriteLine($"Do not save tempertur if it is 0");
            }

        }

        public static double GetTempertur(string TemperatureId)
        {
            // Quick and simple way to find a thermometer and print the temperature
            foreach (var dev in OneWireThermometerDevice.EnumerateDevices())
            {
                if (dev.DeviceId == TemperatureId)
                {
                    return dev.ReadTemperature().DegreesCelsius + _TemperturCalibrateOffSet;
                }
            }

            return 0;
        }

        public static void SetupMaxMinTemperature(MySqlConnection conn)
        {
            int maxTmp = int.Parse(DB.Helper.GetSettingFromDb(conn, "TemperatureMax"));
            int minTmp = int.Parse(DB.Helper.GetSettingFromDb(conn, "TemperatureMin"));
            if ((maxTmp != TemperatureMax) || (minTmp != TemperatureMin))
            {
                TemperatureMax = maxTmp;
                Console.WriteLine($"TemperatureMax is {TemperatureMax}");

                TemperatureMin = minTmp;
                Console.WriteLine($"TemperatureMin is {TemperatureMin}");
            }

        }

    }
}
