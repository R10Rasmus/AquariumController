using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Threading;

namespace AquariumController.Helper
{
    public static class Settings
    {

        public static Timer SetupSaveInterval(MySqlConnection conn, string SettingFromDb, TimerCallback callback)
        {

            // Create saver tempertur timer
            int saveTemperturIntervaleInMin = int.Parse(DB.Helper.GetSettingFromDb(conn, SettingFromDb));
            Console.WriteLine($"{SettingFromDb} is {saveTemperturIntervaleInMin}");

            AutoResetEvent saveAutoResetEvent = new AutoResetEvent(false);
            System.Threading.Timer saveTimer = new System.Threading.Timer(callback, saveAutoResetEvent, 5000, saveTemperturIntervaleInMin * 60 * 1000);
            return saveTimer;
        }






        public static void ReadSetup(Object stateInfo)
        {
            Console.WriteLine("Read settings...");
            var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            localConn.Open();

            Tempertur.SetupMaxMinTemperature(localConn);

            AirPump.SetupAirPumpStartStopTime(localConn);

            localConn.Close();
            localConn.Dispose();
        }



    }
}
