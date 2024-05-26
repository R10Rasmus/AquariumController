using AquariumController.Extension;
using MySql.Data.MySqlClient;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AquariumController.Helper
{
    public class Cooler
    {
        private static DateTime _lastturnOnOff = new DateTime();
        private bool _CoolerOnOff = false;

        private readonly ILocalHueClient client;
        private readonly Light aquariumFanCooler;

        public Cooler(MySqlConnection conn)
        {
            ConsoleEx.WriteLineWithDate($"Getting PhilipsHue Lights...");

            client = new LocalHueClient(DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
            client.Initialize(DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            foreach (Light item in lights)
            {
                ConsoleEx.WriteLineWithDate("name:" + item.Name + " id:" + item.Id);
            }

            aquariumFanCooler = lights.FirstOrDefault(t => t.Name == DB.Helper.GetSettingFromDb(conn, "HeaterName"));

            //make sure cooler is turned off at startup
            TurnCoolerOnOff(false);
        }

        public void CoolerOnOff(MySqlConnection conn)
        {

            if (bool.TryParse(DB.Helper.GetSettingFromDb(conn, "HeaterOnOff"), out bool result) && aquariumFanCooler != null)
            {
                if (_CoolerOnOff != result)
                {
                    _CoolerOnOff = result;

                    if (result)
                    {
                        TurnCoolerOnOff(true);
                    }
                    else
                    {
                        TurnCoolerOnOff(false);
                    }
                }

            }

        }

        public static void SetCoolerControlOnOff(MySqlConnection conn, double _temperature)
        {
            //if the system has cooler and a temperature
            //and if it is more then 5 min since it last was turned on/off, sow we do not now turn on off if the temperature is around max/min
            if (_temperature > 0 && _lastturnOnOff.AddMinutes(5) < DateTime.Now)
            {
                //if temperature is over max, then turn on cooler
                if (_temperature > Tempertur.TemperatureMax)
                {
                    DB.Helper.SaveSettingValue(conn, "HeaterOnOff", true.ToString());

                    _lastturnOnOff = DateTime.Now;
                }

                //if temperature is under TemperatureMax, then turn off cooler
                if (_temperature < Tempertur.TemperatureMax)
                {
                    DB.Helper.SaveSettingValue(conn, "HeaterOnOff", false.ToString());

                    _lastturnOnOff = DateTime.Now;

                }
            }
        }

        private void TurnCoolerOnOff(bool on)
        {
            LightCommand lightCommand = new LightCommand() { On = on };
            client.SendCommandAsync(lightCommand, new List<string> { aquariumFanCooler.Id });

            ConsoleEx.WriteLineWithDate($"Cooler {(on ? "on" : "off")}!");
        }
    }
}
