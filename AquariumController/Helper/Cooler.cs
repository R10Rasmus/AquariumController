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

        private readonly ILocalHueClient client;
        private readonly string FanCoolerName;
        private readonly string ExtraCoolerName;

        public Cooler(MySqlConnection conn)
        {
            ConsoleEx.WriteLineWithDate($"Getting PhilipsHue Lights...");

            client = new LocalHueClient(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
            client.Initialize(Helpers.DB.Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            foreach (Light item in lights)
            {
                ConsoleEx.WriteLineWithDate("name:" + item.Name + " id:" + item.Id);
            }

            FanCoolerName = Helpers.DB.Helper.GetSettingFromDb(conn, "FanCoolerName");
            ExtraCoolerName = Helpers.DB.Helper.GetSettingFromDb(conn, "ExtraCoolerName");

            //make sure cooler is turned off at startup
            TurnExtraCoolerOnOff(false);
            TurnFanCoolerOnOff(false);
        }

        private Light GetLight(string name)
        {
            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            return lights.FirstOrDefault(t => t.Name == name);
        }
        public void CoolerOnOff(MySqlConnection conn)
        {

            if (bool.TryParse(Helpers.DB.Helper.GetSettingFromDb(conn, "FanCoolerrOnOff"), out bool fanResult) && !string.IsNullOrEmpty(FanCoolerName))
            {
                TurnFanCoolerOnOff(fanResult);
            }

            if (bool.TryParse(Helpers.DB.Helper.GetSettingFromDb(conn, "ExtraCoolerOnOff"), out bool extraResult) && !string.IsNullOrEmpty(FanCoolerName))
            {
                TurnExtraCoolerOnOff(extraResult);
            }

        }

        public static void SetCoolerControlOnOff(MySqlConnection conn, double _temperature)
        {
            //if the system has cooler and a temperature
            //and if it is more then 2 min since it last was turned on/off, sow we do not now turn on off if the temperature is around max/min
            if (_temperature > 0 && _lastturnOnOff.AddMinutes(2) < DateTime.Now)
            {
                _lastturnOnOff = DateTime.Now;
                //if temperature is over max, then turn on cooler
                if (_temperature > Tempertur.TemperatureMax)
                {
                    Helpers.DB.Helper.SaveSettingValue(conn, "FanCoolerrOnOff", true.ToString());

                    if (_temperature > Tempertur.TemperatureMax + 0.2)
                    {
                        Helpers.DB.Helper.SaveSettingValue(conn, "ExtraCoolerOnOff", true.ToString());

                    }
                }

                if (_temperature < Tempertur.TemperatureMax + 0.1)
                {
                    Helpers.DB.Helper.SaveSettingValue(conn, "ExtraCoolerOnOff", false.ToString());

                }


                //if temperature is under TemperatureMax, then turn off cooler
                if (_temperature < Tempertur.TemperatureMax)
                {
                    Helpers.DB.Helper.SaveSettingValue(conn, "FanCoolerrOnOff", false.ToString());

                }
            }
        }

        private void TurnFanCoolerOnOff(bool on)
        {
            LightCommand lightCommand = new LightCommand() { On = on };
            var light = GetLight(FanCoolerName);
            if(light == null)
            {
                ConsoleEx.WriteLineWithDate($"Fan Cooler not found!");
                return;
            }
            client.SendCommandAsync(lightCommand, new List<string> { light.Id });

            ConsoleEx.WriteLineWithDate($"Fan Cooler {(on ? "on" : "off")}!");
        }

        private void TurnExtraCoolerOnOff(bool on)
        {

            var light = GetLight(FanCoolerName);
            if (light == null)
            {
                ConsoleEx.WriteLineWithDate($"Extra Cooler not found!");
                return;
            }
            LightCommand lightCommand = new LightCommand() { On = on };
            client.SendCommandAsync(lightCommand, new List<string> { light.Id });

            ConsoleEx.WriteLineWithDate($"Extra Cooler {(on ? "on" : "off")}!");

        }
    }
}
