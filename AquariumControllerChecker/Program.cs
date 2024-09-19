using Helpers.DB;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Diagnostics; // For executing system commands
using System.Globalization;
using System.Reflection; // For retrieving the executable path (if needed)
using System.Threading;

namespace AquariumController
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starter...");

            // Initialize MySQL connection
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString")))
            {
                conn.Open();

                int saveTemperturIntervaleInMin = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureSaveInterval"));
                var checkTime = saveTemperturIntervaleInMin * 2;

                Console.WriteLine($"TemperatureSaveInterva is {saveTemperturIntervaleInMin} and check time is {checkTime}");

                DateTime lastRestart = DateTime.Parse(Helper.GetSettingFromDb(conn, "RestartTime"));
                while (!Console.KeyAvailable)
                {
                    try
                    {
                        // Check if 20 minutes have passed since the last restart
                        if (lastRestart.AddMinutes(20) < DateTime.Now)
                        {

                            string lastDate = Helper.GetLastSettingValue(conn);

                            if (DateTime.TryParse(lastDate, out DateTime lastDateDateTime))
                            {
                                // Check if 5 minutes have passed since the last date
                                if (lastDateDateTime.AddMinutes(checkTime) < DateTime.Now)
                                {
                                    Helper.SaveSettingValue(conn, "RestartTime", DateTime.Now.ToString());
                                    RestartMachine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Failed to parse the lastDate value.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Not time to restart yet.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Got an error: {ex.Message} StackTrace: {ex.StackTrace}");

                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Error InnerException: {ex.InnerException.Message}");
                        }
                    }
                    finally
                    {
                        Thread.Sleep(1000); // Sleep for 1 second before the next check
                    }
                }
            }
        }

        /// <summary>
        /// Restarts the entire machine by executing a system reboot command.
        /// </summary>
        private static void RestartMachine()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Restarting the machine...");

                // Prepare the reboot command
                // For Linux systems, 'shutdown -r now' is commonly used to reboot immediately
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"sudo shutdown -r now\"",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the reboot process
                Process process = Process.Start(startInfo);

                if (process == null)
                {
                    Console.WriteLine("Failed to initiate the reboot process.");
                }
                else
                {
                    // Optionally, wait for the process to exitu
                    process.WaitForExit();
                }

                // Exit the current application
                Environment.Exit(0);
            }
            catch (Exception restartEx)
            {
                Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Failed to restart the machine: {restartEx.Message}");
            }
        }
    }
}
