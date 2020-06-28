using Lcd1602Controller;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;

namespace AquariumController
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("AquariumController is running");

            int _temperatureMax = int.Parse(ConfigurationManager.AppSettings.Get("TemperatureMax"));
            int _temperatureMin = int.Parse(ConfigurationManager.AppSettings.Get("TemperatureMin"));

            using (Lcd1602 lcd = new Lcd1602(registerSelectPin: 20, enablePin: 21, dataPins: new int[] { 26, 19, 13, 06 }, shouldDispose: true))
            {
                ILocalHueClient client = new LocalHueClient(ConfigurationManager.AppSettings.Get("PhilipsHueIp"));
                client.Initialize(ConfigurationManager.AppSettings.Get("PhilipsHuePersonalAppKey"));

                IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();

                foreach (Light item in lights)
                {
                    Console.WriteLine("name:" + item.Name + " id:" +item.Id);
                }

                Light aquariumHeater = lights.FirstOrDefault(t => t.Name == "AquariumHeater");
                LcdConsole console = new LcdConsole(lcd, "A00", false)
                {
                    LineFeedMode = LineWrapMode.Wrap,
                    ScrollUpDelay = new TimeSpan(0, 0, 1)
                };

                FishCharacters(lcd);
                TemperatureCharacters(lcd);
                lcd.SetCursorPosition(0, 0);

                int _fishCount = 0; 
                bool _revers = false;
                int _positionCount = 0;
                int _temperature = 0;
                while (!Console.KeyAvailable)
                {
                    console.ReplaceLine(0, ConfigurationManager.AppSettings.Get("TemperatureText") + _temperature + (char)6);
                    ShowFishOnLine2(console, ref _fishCount, ref _revers, ref _positionCount);

                    HeaterControl(_temperatureMax, _temperatureMin, client, aquariumHeater, _temperature);
                }

                console.Dispose();

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

        private static void HeaterControl(int _temperatureMax, int _temperatureMin, ILocalHueClient client, Light aquariumHeater, int _temperature)
        {
            //if the system has an heater
            if (aquariumHeater != null)
            {
                //if temperature is over max, then turn off heater
                if (_temperature > _temperatureMax)
                {
                    LightCommand lightCommand = new LightCommand() { On = false };
                    client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });
                }

                //if temperature is under min, then turn on heater
                if (_temperature < _temperatureMin)
                {
                    LightCommand lightCommand = new LightCommand() { On = true };
                    client.SendCommandAsync(lightCommand, new List<string> { aquariumHeater.Id });
                }
            }
        }

        private static void ShowFishOnLine2(LcdConsole console, ref int _fishCount, ref bool _revers, ref int _positionCount)
        {
            string tmp = ((char)_fishCount).ToString();
            for (int i = 0; i < _positionCount; i++)
            {
                tmp = " " + tmp;
            }
            console.ReplaceLine(1, tmp);
            Thread.Sleep(1000);
            if (_fishCount == 5)
            {
                _revers = true;
            }
            if (_fishCount == 0)
            {
                _revers = false;
            }

            if (!_revers)
            {
                _fishCount++;
            }
            else
            {
                _fishCount--;
            }

            if (_positionCount < 15)
            {
                _positionCount++;
            }
            else
            {
                _positionCount = 0;
            }
        }

        //private static byte[] hartCharacter()
        //{
        //    byte[] arr = new byte[8];
        //    arr[0] = 0b00000;
        //    arr[1] = 0b01010;
        //    arr[2] = 0b11111;
        //    arr[3] = 0b11111;
        //    arr[4] = 0b11111;
        //    arr[5] = 0b01110;
        //    arr[6] = 0b00100;
        //    arr[7] = 0b00000;

        //    return arr;
        //}


        //private static void CreateWalkCharacters(Lcd1602.Lcd1602 lcd)
        //{
        //    byte[] Walk1 = new byte[8];
        //    Walk1[0] = 0b_00110;
        //    Walk1[1] = 0b_00110;
        //    Walk1[2] = 0b_01100;
        //    Walk1[3] = 0b_10111;
        //    Walk1[4] = 0b_00100;
        //    Walk1[5] = 0b_01110;
        //    Walk1[6] = 0b_01010;
        //    Walk1[7] = 0b_10001;

        //    byte[] Walk2 = new byte[8];
        //    Walk2[0] = 0b_00110;
        //    Walk2[1] = 0b_00110;
        //    Walk2[2] = 0b_01100;
        //    Walk2[3] = 0b_01100;
        //    Walk2[4] = 0b_00110;
        //    Walk2[5] = 0b_00110;
        //    Walk2[6] = 0b_01010;
        //    Walk2[7] = 0b_01010;

        //    // Walk 1
        //    lcd.CreateCustomCharacter(0,
        //       Walk1);
        //    // Walk 2
        //    // Walk 1
        //    lcd.CreateCustomCharacter(1,
        //       Walk2);
        //}

        private static void TemperatureCharacters(Lcd1602 lcd)
        {
            byte[] temperatureCharacters = new byte[8];
            temperatureCharacters[0] = 0b_11000;
            temperatureCharacters[1] = 0b_11000;
            temperatureCharacters[2] = 0b_00011;
            temperatureCharacters[3] = 0b_00100;
            temperatureCharacters[4] = 0b_00100;
            temperatureCharacters[5] = 0b_00100;
            temperatureCharacters[6] = 0b_00011;
            temperatureCharacters[7] = 0b_00000;

            lcd.CreateCustomCharacter(6, temperatureCharacters);
        }

        private static void FishCharacters(Lcd1602 lcd)
        {
            byte[] Fish1 = new byte[8];
            Fish1[0] = 0b_00000;
            Fish1[1] = 0b_00000;
            Fish1[2] = 0b_00000;
            Fish1[3] = 0b_00000;
            Fish1[4] = 0b_00000;
            Fish1[5] = 0b_10110;
            Fish1[6] = 0b_11001;
            Fish1[7] = 0b_10110;

            byte[] Fish2 = new byte[8];
            Fish2[0] = 0b_00000;
            Fish2[1] = 0b_00000;
            Fish2[2] = 0b_00000;
            Fish2[3] = 0b_00000;
            Fish2[4] = 0b_10110;
            Fish2[5] = 0b_11001;
            Fish2[6] = 0b_00110;
            Fish2[7] = 0b_00000;

            byte[] Fish3 = new byte[8];
            Fish3[0] = 0b_00000;
            Fish3[1] = 0b_00000;
            Fish3[2] = 0b_00000;
            Fish3[3] = 0b_00110;
            Fish3[4] = 0b_11001;
            Fish3[5] = 0b_10110;
            Fish3[6] = 0b_00000;
            Fish3[7] = 0b_00000;
            byte[] Fish4 = new byte[8];
            Fish4[0] = 0b_00000;
            Fish4[1] = 0b_00000;
            Fish4[2] = 0b_10110;
            Fish4[3] = 0b_11001;
            Fish4[4] = 0b_10110;
            Fish4[5] = 0b_00000;
            Fish4[6] = 0b_00000;
            Fish4[7] = 0b_00000;

            byte[] Fish5 = new byte[8];
            Fish5[0] = 0b_00000;
            Fish5[1] = 0b_00110;
            Fish5[2] = 0b_11001;
            Fish5[3] = 0b_10110;
            Fish5[4] = 0b_00000;
            Fish5[5] = 0b_00000;
            Fish5[6] = 0b_00000;
            Fish5[7] = 0b_00000;

            byte[] Fish6 = new byte[8];
            Fish6[0] = 0b_10110;
            Fish6[1] = 0b_11001;
            Fish6[2] = 0b_00110;
            Fish6[3] = 0b_00000;
            Fish6[4] = 0b_00000;
            Fish6[5] = 0b_00000;
            Fish6[6] = 0b_00000;
            Fish6[7] = 0b_00000;

            lcd.CreateCustomCharacter(0,Fish1);
            lcd.CreateCustomCharacter(1, Fish2);
            lcd.CreateCustomCharacter(2, Fish3);
            lcd.CreateCustomCharacter(3, Fish4);
            lcd.CreateCustomCharacter(4, Fish5);
            lcd.CreateCustomCharacter(5, Fish6);
        }

     
    }
}
