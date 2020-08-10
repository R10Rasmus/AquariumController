using AquariumController.Extension;
using Lcd1602Controller;
using MySql.Data.MySqlClient;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AquariumController.Helper
{
    public class Heater
    {
        private static DateTime _lastturnOnOff = new DateTime();
        private bool _HeaterOnOff = false;

        private readonly ILocalHueClient client;
        private readonly Light aquariumHeater;

        public Heater(MySqlConnection conn)
        {
            ConsoleEx.WriteLineWithDate($"Getting PhilipsHue Lights...");

            client = new LocalHueClient(DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
            client.Initialize(DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            foreach (Light item in lights)
            {
                ConsoleEx.WriteLineWithDate("name:" + item.Name + " id:" + item.Id);
            }

            aquariumHeater = lights.FirstOrDefault(t => t.Name == DB.Helper.GetSettingFromDb(conn, "HeaterName"));

            //make sure heater is turned off at startup
            TurnHeaterOnOff(false);
        }

        public void HeaterOnOff(MySqlConnection conn)
        {

            if (bool.TryParse(DB.Helper.GetSettingFromDb(conn, "HeaterOnOff"), out bool result) && aquariumHeater != null)
            {
                if (_HeaterOnOff != result)
                {
                    _HeaterOnOff = result;

                    if (result)
                    {
                        TurnHeaterOnOff(true);
                    }
                    else
                    {
                        TurnHeaterOnOff(false);
                    }
                }

            }

        }

        public static void SetHeaterControlOnOff(MySqlConnection conn, double _temperature)
        {
            //if the system has an heater and a temperature
            //and if it is more then 5 min since it last was turned on/off, sow we do not now turn on off if the temperature is around max/min
            if (_temperature > 0 && _lastturnOnOff.AddMinutes(5) < DateTime.Now)
            {
                //if temperature is over max, then turn off heater
                if (_temperature > Tempertur.TemperatureMax)
                {
                    DB.Helper.SaveSettingValue(conn, "HeaterOnOff", false.ToString());

                    _lastturnOnOff = DateTime.Now;
                }

                //if temperature is under min, then turn on heater
                if (_temperature < Tempertur.TemperatureMin)
                {
                    DB.Helper.SaveSettingValue(conn, "HeaterOnOff", true.ToString());

                    _lastturnOnOff = DateTime.Now;

                }
            }
        }

        private void TurnHeaterOnOff(bool on)
        {
            LightCommand lightCommand = new LightCommand() { On = on };
            client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });

            ConsoleEx.WriteLineWithDate($"Heater {(on?"on":"off")}!");
        }
    }
}
