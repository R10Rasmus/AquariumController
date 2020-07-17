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
using System.Device.Gpio;
using System.Device.I2c;
using System.Linq;
using System.Threading;

namespace AquariumController
{
    class Program
    {
        const double _TemperturCalibrateOffSet = 0.6;

        const int AIRPUMPPIN = 01;

        const int LCDRSPIN = 07;
        const int LCDENABLEPIN = 08;
        static readonly int[] LCDDATA = { 06, 13, 19, 26 };

        const int BUSID = 1;
        const int I2CADDRESS = 0x3F;

        static double _Tempertur = 0;
        static double _TemperatureMin = 0;
        static double _TemperatureMax = 0;
        static double _PH = 0;
        static bool _AirPumpOnOff = false;
        static bool _TimerAirPumpOnOff = false;


        static DateTime _AirPumpStart;
        static DateTime _AirPumpStop;

        static GpioController _Controller;

        static void Main(string[] args)
        {
            Console.WriteLine("AquariumController is running");

            I2cConnectionSettings settings = new I2cConnectionSettings(BUSID, I2CADDRESS);
            I2cDevice device = I2cDevice.Create(settings);

            Console.WriteLine("Setting up UFire EC Probe...");
            Iot.Device.UFire.UFire_pH uFire_pH = new Iot.Device.UFire.UFire_pH(device);

            Console.WriteLine("Setting up MySql db");
            MySqlConnection conn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            conn.Open();

            SetupHeater(conn, out ILocalHueClient client, out Light aquariumHeater);

          //  readSetup(new object());

            Timer saveTemperturTimer = SetupSaveInterval(conn, "TemperatureSaveInterval", saveTempertur);
            Timer savePhTimer = SetupSaveInterval(conn, "PHSaveInterval", savePh);

            AutoResetEvent saveTemperturAutoResetEvent = new AutoResetEvent(false);
            Timer readSetupTimer = new Timer(readSetup, saveTemperturAutoResetEvent, 5000, 1 * 60 * 1000);

            _Controller = new GpioController();
            _Controller.OpenPin(AIRPUMPPIN, PinMode.Output);

            using (Lcd1602 lcd = new Lcd1602(registerSelectPin: LCDRSPIN, enablePin: LCDENABLEPIN, dataPins: LCDDATA, shouldDispose: true))
            {

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

                while (!Console.KeyAvailable)
                {
                    _Tempertur = getTempertur(Helper.GetSettingFromDb(conn, "WaterTemperatureId"));

                    _PH = Math.Round(uFire_pH.MeasurepH(Convert.ToSingle(_Tempertur)), 1);

                    string tempterturText = Math.Round(_Tempertur, 1, MidpointRounding.AwayFromZero).ToString() + (char)SetCharacters.TemperatureCharactersNumber;

                    string pHText = _PH + "pH";

                    console.ReplaceLine(0, tempterturText + " " + pHText);

                    Animation.ShowFishOnLine2(console, ref _fishCount, ref _revers, ref _positionCount);

                   // Console.WriteLine("_fishCount:" + _fishCount + " _revers:+" + _revers + " _positionCount:" + _positionCount);

                    HeaterControl(client, aquariumHeater, _Tempertur, console);

                    SetAirPumpOnOff(conn);
                    AirPumpOnOff(conn);

                    Thread.Sleep(1000);
                }

                console.Dispose();
            }

            saveTemperturTimer.Dispose();
            savePhTimer.Dispose();
            readSetupTimer.Dispose();

            conn.Close();
            conn.Dispose();

            _Controller.Dispose();


        }

        private static Timer SetupSaveInterval(MySqlConnection conn, string SettingFromDb, TimerCallback callback)
        {
            // Create saver tempertur timer
            int saveTemperturIntervaleInMin = int.Parse(Helper.GetSettingFromDb(conn, SettingFromDb));
            Console.WriteLine($"{SettingFromDb} is {saveTemperturIntervaleInMin}");

            AutoResetEvent saveAutoResetEvent = new AutoResetEvent(false);
            System.Threading.Timer saveTimer = new System.Threading.Timer(callback, saveAutoResetEvent, 5000, saveTemperturIntervaleInMin * 60 * 1000);
            return saveTimer;
        }



        private static void SetupAirPumpStartStopTime(MySqlConnection conn)
        {
            string timeStart = Helper.GetSettingFromDb(conn, "AirPumpStart");
            string timeStop = Helper.GetSettingFromDb(conn, "AirPumpStop");

            if (!string.IsNullOrWhiteSpace(timeStart))
            {
                _AirPumpStart = DateTime.Parse(timeStart);

                Console.WriteLine($"AirPumpStart is {_AirPumpStart.TimeOfDay}");
            }

            if (!string.IsNullOrWhiteSpace(timeStop))
            {
                _AirPumpStop = DateTime.Parse(timeStop);

                Console.WriteLine($"AirPumpStop is {_AirPumpStop.TimeOfDay}");
            }
        }

        private static void SetupMaxMinTemperature(MySqlConnection conn)
        {
            int maxTmp = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureMax"));
            int minTmp = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureMin"));
            if ((maxTmp != _TemperatureMax) || (minTmp  != _TemperatureMin))
            {
                _TemperatureMax = maxTmp;
                Console.WriteLine($"TemperatureMax is {_TemperatureMax}");

                _TemperatureMin = minTmp;
                Console.WriteLine($"TemperatureMin is {_TemperatureMin}");
            }
          
        }

        private static void SetupHeater(MySqlConnection conn, out ILocalHueClient client, out Light aquariumHeater)
        {
            Console.WriteLine($"Getting PhilipsHue Lights...");

            client = new LocalHueClient(Helper.GetSettingFromDb(conn, "PhilipsHueIp"));
            client.Initialize(Helper.GetSettingFromDb(conn, "PhilipsHuePersonalAppKey"));

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

            foreach (Light item in lights)
            {
                Console.WriteLine("name:" + item.Name + " id:" + item.Id);
            }

            aquariumHeater = lights.FirstOrDefault(t => t.Name == Helper.GetSettingFromDb(conn, "HeaterName"));
        }

        private static void SetAirPumpOnOff(MySqlConnection conn)
        {
            if (DateTime.Now.TimeOfDay >= _AirPumpStart.TimeOfDay)
            {
                if(_TimerAirPumpOnOff != true)
                {
                    _TimerAirPumpOnOff = true;
                    Helper.SaveSettingValue(conn, "airPumpOnOff", true.ToString());
                }
                
            }

            if (DateTime.Now.TimeOfDay >= _AirPumpStop.TimeOfDay)
            {
                if (_TimerAirPumpOnOff != false)
                {
                    _TimerAirPumpOnOff = false;
                    Helper.SaveSettingValue(conn, "airPumpOnOff", false.ToString());
                }
            }
        }

        private static void AirPumpOnOff(MySqlConnection conn)
        {

            if (bool.TryParse(Helper.GetSettingFromDb(conn, "AirPumpOnOff"), out bool result))
            {
                if(_AirPumpOnOff != result)
                {
                    _AirPumpOnOff = result;

                    if (result)
                    {

                        _Controller.Write(AIRPUMPPIN, PinValue.High);
                        Console.WriteLine($"Air pump on!");
                    }
                    else
                    {
                        _Controller.Write(AIRPUMPPIN, PinValue.Low);

                        Console.WriteLine($"Air pump off!");
                    }
                }
                
            }

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
        private static void savePh(Object stateInfo)
        {
            if (_PH > 0)
            {
                Console.WriteLine($"Save ph with value {_PH}");

                var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                Helper.SaveChannelValue(localConn, "ph", _PH);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                Console.WriteLine($"Do not save pH if it is 0");
            }

        }

        private static void readSetup(Object stateInfo)
        {
            Console.WriteLine("Read settings...");
            var localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            localConn.Open();

            SetupMaxMinTemperature(localConn);

            SetupAirPumpStartStopTime(localConn);

            localConn.Close();
            localConn.Dispose();
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

        private static void HeaterControl(ILocalHueClient client, Light aquariumHeater, double _temperature, LcdConsole console)
        {
            //if the system has an heater and a temperature
            if (aquariumHeater != null && _temperature >0)
            {
                //if temperature is over max, then turn off heater
                if (_temperature > _TemperatureMax)
                {
                    LightCommand lightCommand = new LightCommand() { On = false };
                    client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });

                    Console.WriteLine($"Heater off!");

                    console.BlinkDisplay(2);
                }

                //if temperature is under min, then turn on heater
                if (_temperature < _TemperatureMin)
                {
                    LightCommand lightCommand = new LightCommand() { On = true };
                    client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });

                    Console.WriteLine($"Heater on!");
                }
            }
        }

      

      

     
    }
}
