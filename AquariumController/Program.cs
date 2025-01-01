using AquariumController.Display;
using AquariumController.Extension;
using AquariumController.Helper;
using Lcd1602Controller;
using MySql.Data.MySqlClient;
using Renci.SshNet.Messages;
using System;
using System.Configuration;
using System.Device.Gpio;
using System.IO;
using System.Linq;
using System.Threading;
using UnitsNet;

namespace AquariumController
{
    class Program
    {

       // const int AIRPUMPPIN = 01;

        const int LCDRSPIN = 07;
        const int LCDENABLEPIN = 08;
        static readonly int[] LCDDATA = { 06, 13, 19, 26 };
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

            ConsoleEx.WriteLineWithDate("Getting devices...");

            var directories = Tempertur.GetDirectories();

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

                var temperatur = Tempertur.ReadTemperatur(directories);
                Tempertur.TemperturValue = temperatur.FirstOrDefault();

                SendSMS.SendSMSAsync(Tempertur.TemperturValue, conn, $"Restarted tempertur is {Tempertur.TemperturValue}");

                while (!Console.KeyAvailable)
                {
                    try
                    {

                        temperatur = Tempertur.ReadTemperatur(directories);
                        Tempertur.TemperturValue = temperatur.FirstOrDefault();

                        if(Tempertur.TemperturValue == 0)
                        {
                            ConsoleEx.WriteLineWithDate("Tempertur is 0, trying to read again");
                            Thread.Sleep(1000); // wait 1 sec
                            directories = Tempertur.GetDirectories();
                            temperatur = Tempertur.ReadTemperatur(directories);
                            Tempertur.TemperturValue = temperatur.FirstOrDefault();
                        }
                        string tempterturText =string.Empty;
                        double roundTemp = 0;
                        if (Tempertur.TemperturValue != 0)
                        {

                            //if tempertur is over 26.35 or under 25,8 send sms
                            if (Tempertur.TemperturValue > 26.35 || (Tempertur.TemperturValue < 25.8))
                            {

                                if (Tempertur.TemperturValue > 26.35)
                                    SendSMS.SendSMSAsync(Tempertur.TemperturValue, conn, $"Tempertur is to highe Tempertur {Tempertur.TemperturValue}");
                                else
                                    SendSMS.SendSMSAsync(Tempertur.TemperturValue, conn, $"Tempertur is to low Tempertur {Tempertur.TemperturValue}");
                            }
                            if (Tempertur.TemperturValue > 27)
                            {
                                SendSMS.SendSMSAsync(Tempertur.TemperturValue, conn, $"ALARM!! Temperature is WAY too high.{Tempertur.TemperturValue}", true);
                            }

                            Cooler.SetCoolerControlOnOff(conn, Tempertur.TemperturValue);
                            cooler.CoolerOnOff(conn);

                            roundTemp = Math.Round(Tempertur.TemperturValue, 2, MidpointRounding.AwayFromZero);

                            tempterturText = roundTemp.ToString() + (char)SetCharacters.TemperatureCharactersNumber;
                        }
                        else
                        {
                            ConsoleEx.WriteLineWithDate("Tempertur is still 0, trying to read again");
                            tempterturText = "ERROR! Could not read tempertur, trying again...";

                            roundTemp = Tempertur.TemperatureMax + 0.2;

                        }
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

        }

    }
}
