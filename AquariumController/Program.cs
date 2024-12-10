using AquariumController.Display;
using AquariumController.Extension;
using AquariumController.Helper;
using Lcd1602Controller;
using MySql.Data.MySqlClient;
using Rinsen.IoT.OneWire;
using System;
using System.Configuration;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AquariumController
{
    class Program
    {

        const int AIRPUMPPIN = 01;

        const int LCDRSPIN = 07;
        const int LCDENABLEPIN = 08;
        static readonly int[] LCDDATA = { 06, 13, 19, 26 };
        static GpioController _Controller;

        // Base path where the device directories are located
        static string basePath = "/sys/bus/w1/devices/";

        static void Main(string[] args)
        {
            Cooler cooler = null;

            ConsoleEx.WriteLineWithDate("AquariumController is running");


            ConsoleEx.WriteLineWithDate("Setting up MySql db....");
            MySqlConnection conn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
            conn.Open();

            ConsoleEx.WriteLineWithDate("Setting up Heater....");
            cooler = new Cooler(conn);

            Timer saveTemperturTimer = Settings.SetupSaveInterval(conn, "TemperatureSaveInterval", Tempertur.SaveTempertur);

            //read setting every 5 minute.
            AutoResetEvent saveTemperturAutoResetEvent = new AutoResetEvent(false);
            Timer readSetupTimer = new Timer(Settings.ReadSetup, saveTemperturAutoResetEvent, 0, 5 * 60 * 1000);

            ConsoleEx.WriteLineWithDate("Setting up GpioController....");
            _Controller = new GpioController();
            _Controller.OpenPin(AIRPUMPPIN, PinMode.Output);

            ConsoleEx.WriteLineWithDate("Getting devices...");
            System.Collections.Generic.IEnumerable<string> directories = null;
            try
            {
                // Get all directories in the base path that start with "28"
                directories = Directory.GetDirectories(basePath)
                                          .Where(dir => Path.GetFileName(dir).StartsWith("28"));

                if (!directories.Any())
                {
                    Console.WriteLine("No device directories starting with '28' were found.");
                    return;
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"[Error] The directory {basePath} does not exist.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"[Error] Access to the directory {basePath} is denied.");
            }

            ConsoleEx.WriteLineWithDate($"Found {directories.Count()} devices");

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

                        var temperatur = Tempertur.ReadTemperatur(directories);


                        Tempertur.TemperturValue = temperatur.FirstOrDefault();
                        //if tempertur is over 25.35 or under 24,35 send sms
                        if (Tempertur.TemperturValue > 25.35 || (Tempertur.TemperturValue < 24.35 && Tempertur.TemperturValue > 20) )
                        {
                            string SMSapiToken = Helpers.DB.Helper.GetSettingFromDb(conn, "SMSapiToken");
                            string PhonNumber = Helpers.DB.Helper.GetSettingFromDb(conn, "PhonNumber");
                            SendSMS.SendSMSAsync(Tempertur.TemperturValue, SMSapiToken, PhonNumber);
                        }

                        Cooler.SetCoolerControlOnOff(conn, Tempertur.TemperturValue);
                        cooler.CoolerOnOff(conn);

                        var roundTemp = Math.Round(Tempertur.TemperturValue, 3, MidpointRounding.AwayFromZero);

                        string tempterturText = roundTemp.ToString() + (char)SetCharacters.TemperatureCharactersNumber;

                        console.ReplaceLine(0, tempterturText );

                        Animation.ShowFishOnLine2(console, ref _fishCount, ref _revers, ref _positionCount);

                        //Blink display if tempertur is over max tempertur
                        if (roundTemp  > Tempertur.TemperatureMax + 0.2)
                        {
                            console.BlinkDisplay(1);
                        }

                    }
                    catch (Exception ex)
                    {
                        console.ReplaceLine(0, "ERROR! Check tempertur and restart");
                        ConsoleEx.WriteLineWithDate("Got an error: " + ex.Message + "StackTrace: " + ex.StackTrace);
                        Fails.SaveFail(ex);
                        if (ex.InnerException != null)
                        {
                            ConsoleEx.WriteLineWithDate("Error InnerException: " + ex.InnerException.Message);
                        }

                    }
                    finally
                    {
                        Thread.Sleep(1000);
                    }

                }

                console.Dispose();

            }

            saveTemperturTimer.Dispose();
            readSetupTimer.Dispose();

            conn.Close();
            conn.Dispose();

            _Controller.Dispose();


        }

    }
}
