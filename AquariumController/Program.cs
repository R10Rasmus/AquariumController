using AquariumController.Display;
using AquariumController.Extension;
using AquariumController.Helper;
using Lcd1602Controller;
using MySql.Data.MySqlClient;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Configuration;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;

namespace AquariumController
{
    class Program
    {

        const int AIRPUMPPIN = 01;

        const int LCDRSPIN = 07;
        const int LCDENABLEPIN = 08;
        static readonly int[] LCDDATA = { 06, 13, 19, 26 };

        const int BUSID = 1;
        const int I2CADDRESS = 0x3F;

        static GpioController _Controller;

        static void Main(string[] args)
        {
            Heater heater = null;

            ConsoleEx.WriteLineWithDate("AquariumController is running");

            ConsoleEx.WriteLineWithDate("Setting up I2C...");
            I2cConnectionSettings settings = new I2cConnectionSettings(BUSID, I2CADDRESS);
            I2cDevice device = I2cDevice.Create(settings);

            ConsoleEx.WriteLineWithDate("Setting up UFire EC Probe...");
            Iot.Device.UFire.UFire_pH uFire_pH = new Iot.Device.UFire.UFire_pH(device);
            uFire_pH.UseTemperatureCompensation(true);

            ConsoleEx.WriteLineWithDate("Setting up MySql db....");
            MySqlConnection conn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            conn.Open();

            ConsoleEx.WriteLineWithDate("Setting up Heater....");
            heater = new Heater(conn);

            Timer saveTemperturTimer = Settings.SetupSaveInterval(conn, "TemperatureSaveInterval", Tempertur.SaveTempertur);
            Timer savePhTimer = Settings.SetupSaveInterval(conn, "PHSaveInterval", Ph.SavePh);

            //read setting every 5 minute.
            AutoResetEvent saveTemperturAutoResetEvent = new AutoResetEvent(false);          
            Timer readSetupTimer = new Timer(Settings.ReadSetup, saveTemperturAutoResetEvent, 0, 5 * 60 * 1000);

            ConsoleEx.WriteLineWithDate("Setting up GpioController....");
            _Controller = new GpioController();
            _Controller.OpenPin(AIRPUMPPIN, PinMode.Output);

            ConsoleEx.WriteLineWithDate("Setting up Lcd1602....");
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
                    try
                    {
                        
                       Tempertur.TemperturValue = Convert.ToDouble( uFire_pH.MeasureTemp())+ Tempertur.TemperturCalibrateOffSet;

                        Ph.PH = Math.Round(uFire_pH.MeasurepH(), 1);

                        string tempterturText = Math.Round(Tempertur.TemperturValue, 1, MidpointRounding.AwayFromZero).ToString() + (char)SetCharacters.TemperatureCharactersNumber;

                        string pHText = Ph.PH + "pH";

                        console.ReplaceLine(0, tempterturText + " " + pHText);

                        Animation.ShowFishOnLine2(console, ref _fishCount, ref _revers, ref _positionCount);

                        Heater.SetHeaterControlOnOff(conn, Tempertur.TemperturValue);
                        heater.HeaterOnOff(conn);

                        //Blink display if tempertur is over max tempertur
                        if (Tempertur.TemperturValue > Tempertur.TemperatureMax)
                        {
                            console.BlinkDisplay(1);
                        }


                        AirPump.AirPumpOnOff(conn, _Controller, AIRPUMPPIN);

                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    {
                        ConsoleEx.WriteLineWithDate("Got an error: " + ex.Message + "StackTrace: " + ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            ConsoleEx.WriteLineWithDate("Error InnerException: " + ex.InnerException.Message);
                        }

                    }
                    finally
                    {
                        Thread.Sleep(1000);
                    }
#pragma warning restore CA1031 // Do not catch general exception types

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

    }
}
