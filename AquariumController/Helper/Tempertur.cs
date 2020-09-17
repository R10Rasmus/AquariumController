using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;

namespace AquariumController.Helper
{
    public static class Tempertur
    {
        public const double TemperturCalibrateOffSet = 0.9;

        public static double TemperturValue = 0;
        public static double TemperatureMin = 0;
        public static double TemperatureMax = 0;
        public static void SaveTempertur(Object stateInfo)
        {
            if (TemperturValue > 0)
            {
                ConsoleEx.WriteLineWithDate($"Save tempertur with value {TemperturValue}");

                var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                DB.Helper.SaveChannelValue(localConn, "temperature", TemperturValue);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                ConsoleEx.WriteLineWithDate($"Do not save tempertur if it is 0");
            }

        }

        public static void SetupMaxMinTemperature(MySqlConnection conn)
        {
            int maxTmp = int.Parse(DB.Helper.GetSettingFromDb(conn, "TemperatureMax"));
            int minTmp = int.Parse(DB.Helper.GetSettingFromDb(conn, "TemperatureMin"));
            if ((maxTmp != TemperatureMax) || (minTmp != TemperatureMin))
            {
                TemperatureMax = maxTmp;
                ConsoleEx.WriteLineWithDate($"TemperatureMax is {TemperatureMax}");

                TemperatureMin = minTmp;
                ConsoleEx.WriteLineWithDate($"TemperatureMin is {TemperatureMin}");
            }

        }

    }
}
