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
        private bool _FanCoolerOnOff = false;
        private bool _ExtraCoolerOnOff = false;

        private readonly ILocalHueClient client;
        private readonly Light aquariumFanCooler;
        private readonly Light aquariumExtraCooler;

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

            aquariumFanCooler = lights.FirstOrDefault(t => t.Name == Helpers.DB.Helper.GetSettingFromDb(conn, "FanCoolerName"));
            aquariumExtraCooler = lights.FirstOrDefault(t => t.Name == Helpers.DB.Helper.GetSettingFromDb(conn, "ExtraCoolerName"));

            //make sure cooler is turned off at startup
            TurnExtraCoolerOnOff(false);
            TurnFanCoolerOnOff(false);
        }

        public void CoolerOnOff(MySqlConnection conn)
        {

            if (bool.TryParse(Helpers.DB.Helper.GetSettingFromDb(conn, "FanCoolerrOnOff"), out bool fanResult) && aquariumFanCooler != null)
            {
                if (_FanCoolerOnOff != fanResult)
                {
                    _FanCoolerOnOff = fanResult;
                    TurnFanCoolerOnOff(fanResult);
                }
            }

            if (bool.TryParse(Helpers.DB.Helper.GetSettingFromDb(conn, "ExtraCoolerOnOff"), out bool extraResult) && aquariumFanCooler != null)
            {
                if (_ExtraCoolerOnOff != extraResult)
                {
                    _ExtraCoolerOnOff = extraResult;
                    TurnExtraCoolerOnOff(extraResult);
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
                    Helpers.DB.Helper.SaveSettingValue(conn, "FanCoolerrOnOff", true.ToString());

                    _lastturnOnOff = DateTime.Now;
                }

                
                if (_temperature > Tempertur.TemperatureMax+0.2)
                {
                    Helpers.DB.Helper.SaveSettingValue(conn, "ExtraCoolerOnOff", true.ToString());

                    _lastturnOnOff = DateTime.Now;
                }

                if (_temperature < Tempertur.TemperatureMax+0.1)
                {
                    Helpers.DB.Helper.SaveSettingValue(conn, "ExtraCoolerOnOff", false.ToString());

                    _lastturnOnOff = DateTime.Now;

                }

                //if temperature is under TemperatureMax, then turn off cooler
                if (_temperature < Tempertur.TemperatureMax)
                {
                    Helpers.DB.Helper.SaveSettingValue(conn, "FanCoolerrOnOff", false.ToString());

                    _lastturnOnOff = DateTime.Now;

                }
            }
        }

        private void TurnFanCoolerOnOff(bool on)
        {
            LightCommand lightCommand = new LightCommand() { On = on };
            client.SendCommandAsync(lightCommand, new List<string> { aquariumFanCooler.Id });

            ConsoleEx.WriteLineWithDate($"Fan Cooler {(on ? "on" : "off")}!");
        }

        private void TurnExtraCoolerOnOff(bool on)
        {
            if (aquariumExtraCooler != null)
            {
                LightCommand lightCommand = new LightCommand() { On = on };
                client.SendCommandAsync(lightCommand, new List<string> { aquariumExtraCooler.Id });

                ConsoleEx.WriteLineWithDate($"Extra Cooler {(on ? "on" : "off")}!");
            }

           
        }
    }
}
