using AquariumController.DB;
using AquariumController.Display;
using Iot.Device.OneWire;
using Lcd1602Controller;
using MySql.Data.MySqlClient;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Timers;

namespace AquariumController
{
    class Program
    {
      

        static double _Tempertur = 0;

        static double _TemperturCalibrateOffSet = 0.6;
        static void Main(string[] args)
        {
           
            Console.WriteLine("AquariumController is running");

            int _temperatureMax = 0;
            int _temperatureMin = 0;

            MySqlConnection conn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));

            conn.Open();

            _temperatureMax = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureMax"));
            Console.WriteLine($"TemperatureMax is {_temperatureMax}");

            _temperatureMin = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureMin"));
            Console.WriteLine($"TemperatureMin is {_temperatureMin}");

            using (Lcd1602 lcd = new Lcd1602(registerSelectPin: 07, enablePin: 08, dataPins: new int[] { 25, 24, 23, 18 }, shouldDispose: true))
            {
                ILocalHueClient client = new LocalHueClient(Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
                client.Initialize(Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

                IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

                foreach (Light item in lights)
                {
                    Console.WriteLine("name:" + item.Name + " id:" + item.Id);
                }

                Light aquariumHeater = lights.FirstOrDefault(t => t.Name == Helper.GetSettingFromDb(conn, "HeaterName"));
                LcdConsole console = new LcdConsole(lcd, "A00", false)
                {
                    LineFeedMode = LineWrapMode.Wrap,
                    ScrollUpDelay = new TimeSpan(0, 0, 1)
                };

                SetCharacters.FishCharacters(lcd);
                SetCharacters.TemperatureCharacters(lcd);
                lcd.SetCursorPosition(0, 0);

                int _fishCount = 0;
                bool _revers = false;
                int _positionCount = 0;

                // Create saver tempertur timer
                int saveTemperturIntervaleInMin = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureSaveInterval"));
                Console.WriteLine($"TemperatureSaveInterval is {saveTemperturIntervaleInMin}");
                AutoResetEvent saveTemperturAutoResetEvent = new AutoResetEvent(false);
                System.Threading.Timer saveTemperturTimer = new System.Threading.Timer(saveTempertur, saveTemperturAutoResetEvent, 5000, saveTemperturIntervaleInMin * 60 * 1000);

                while (!Console.KeyAvailable)
                {
                    _Tempertur = getTempertur(Helper.GetSettingFromDb(conn, "WaterTemperatureId"));

                    string tempterturText = Helper.GetSettingFromDb(conn, "TemperatureText") + Math.Round(_Tempertur,0) + (char)SetCharacters.TemperatureCharactersNumber;

                    console.ReplaceLine(0, tempterturText);
                    
                    Animation.ShowFishOnLine2(console, ref _fishCount, ref _revers, ref _positionCount);
                    Thread.Sleep(1000);

                    HeaterControl(_temperatureMax, _temperatureMin, client, aquariumHeater, _Tempertur, console);

                }


             //   _ = saveTemperturAutoResetEvent.WaitOne();
                saveTemperturTimer.Dispose();

                console.Dispose();
                conn.Close();
                conn.Dispose();

            }

            //int pinOut = 4;
            //int pinIn = 21;

            //GpioController controller = new GpioController();
            //controller.OpenPin(pinOut, PinMode.Output);
            //controller.OpenPin(pinIn, PinMode.InputPullDown);

            //while (!Console.KeyAvailable)
            //{
            //    if(controller.Read(pinIn) == PinValue.High)
            //    {
            //        Console.WriteLine("on!");
            //        controller.Write(pinOut, PinValue.High);
            //    }
            //    else
            //    {
            //        Console.WriteLine("off!");
            //        controller.Write(pinOut, PinValue.Low);
            //    }
            //}

            //controller.Dispose();
        }

        private static void saveTempertur(Object stateInfo)
        {
            if(_Tempertur>0)
            {
                Console.WriteLine($"Save tempertur with value {_Tempertur}");

                var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                Helper.SaveChannelValue(localConn, "temperature", _Tempertur);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                Console.WriteLine($"Do not save tempertur if it is 0");
            }

        }

       private static double getTempertur(string TemperatureId)
        {
            // Quick and simple way to find a thermometer and print the temperature
            foreach (var dev in OneWireThermometerDevice.EnumerateDevices())
            {
                if(dev.DeviceId == TemperatureId)
                {
                    return dev.ReadTemperature().DegreesCelsius+ _TemperturCalibrateOffSet;
                }
            }

            return 0;
        }

        private static void HeaterControl(int _temperatureMax, int _temperatureMin, ILocalHueClient client, Light aquariumHeater, double _temperature, LcdConsole console)
        {
            //if the system has an heater
            if (aquariumHeater != null)
            {
                //if temperature is over max, then turn off heater
                if (_temperature > _temperatureMax)
                {
                    LightCommand lightCommand = new LightCommand() { On = false };
                    client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });
                    console.BlinkDisplay(2);
                }

                //if temperature is under min, then turn on heater
                if (_temperature < _temperatureMin)
                {
                    LightCommand lightCommand = new LightCommand() { On = true };
                    client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });
                }
            }
        }

      

      

     
    }
}
