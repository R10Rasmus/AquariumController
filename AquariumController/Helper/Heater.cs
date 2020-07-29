using Lcd1602Controller;
using MySql.Data.MySqlClient;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AquariumController.Helper
{
    public static class Heater
    {
        static bool _HeaterOnOff = false;

        public static void SetupHeater(MySqlConnection conn, out ILocalHueClient client, out Light aquariumHeater)
        {
            Console.WriteLine($"Getting PhilipsHue Lights...");

            client = new LocalHueClient(DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
            client.Initialize(DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            foreach (Light item in lights)
            {
                Console.WriteLine("name:" + item.Name + " id:" + item.Id);
            }

            aquariumHeater = lights.FirstOrDefault(t => t.Name == DB.Helper.GetSettingFromDb(conn, "HeaterName"));
        }

        public static void HeaterOnOff(MySqlConnection conn, ILocalHueClient client, Light aquariumHeater)
        {

            if (bool.TryParse(DB.Helper.GetSettingFromDb(conn, "HeaterOnOff"), out bool result) && aquariumHeater != null)
            {
                if (_HeaterOnOff != result)
                {
                    _HeaterOnOff = result;

                    if (result)
                    {

                        LightCommand lightCommand = new LightCommand() { On = true };
                        client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });

                        Console.WriteLine($"Heater on!");
                    }
                    else
                    {
                        LightCommand lightCommand = new LightCommand() { On = false };
                        client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });

                        Console.WriteLine($"Heater off!");
                    }
                }

            }

        }

        public static void SetHeaterControlOnOff(MySqlConnection conn, double _temperature, LcdConsole console)
        {
            //if the system has an heater and a temperature
            if (_temperature > 0)
            {
                //if temperature is over max, then turn off heater
                if (_temperature > Tempertur.TemperatureMax)
                {
                    DB.Helper.SaveSettingValue(conn, "HeaterOnOff", false.ToString());

                    console.BlinkDisplay(2);
                }

                //if temperature is under min, then turn on heater
                if (_temperature < Tempertur.TemperatureMin)
                {
                    DB.Helper.SaveSettingValue(conn, "HeaterOnOff", true.ToString());
                    
                }
            }
        }
    }
}
