using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;

namespace AquariumController.Helper
{
    public static class Ph
    {

        public static double PH = 0;

        static bool _isFirstSave = true;
        public static void SavePh(Object stateInfo)
        {
            //do not save first value, pH value needs to stabilize
            if (_isFirstSave)
            {
                _isFirstSave = false;
                ConsoleEx.WriteLineWithDate($"Do not save first pH value, value is {PH}");
                return;
            }

            if (PH > 0)
            {
                ConsoleEx.WriteLineWithDate($"Save ph with value {PH}");

                MySqlConnection localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                DB.Helper.SaveChannelValue(localConn, "ph", PH);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                ConsoleEx.WriteLineWithDate($"Do not save pH if it is 0");
            }

        }

    }
}
