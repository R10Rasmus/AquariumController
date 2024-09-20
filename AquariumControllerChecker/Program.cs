using Google.Protobuf;
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

            string format = "dd-MM-yyyy HH:mm:ss";
            CultureInfo provider = CultureInfo.InvariantCulture;

            MySqlConnection conn = null;

            // Retry logic for establishing the database connection
            int counter = 0;
            while (true)
            {
                counter++;
                try
                {
                    string connectionString = ConfigurationManager.AppSettings.Get("ConnectionString");
                    conn = new MySqlConnection(connectionString);
                    conn.Open();
                    Console.WriteLine("Connected to the database.");
                    break; // Exit the loop if connection is successful
                }
                catch (Exception ex)
                {
                    if(counter > 5)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Failed to connect to the database: {ex.Message}");
                        Console.WriteLine("Exiting the application...");
                        Environment.Exit(1); // Exit the application if the connection fails after 5 attempts
                    }
                    Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Failed to connect to the database: {ex.Message}");
                    Console.WriteLine("Retrying in 1 minute...");
                    Thread.Sleep(TimeSpan.FromMinutes(1)); // Wait for 1 minute before retrying
                }
            }

            using (conn)
            {
                try
                {
                    int saveTemperatureIntervalInMin = int.Parse(Helper.GetSettingFromDb(conn, "TemperatureSaveInterval"));
                    var checkTime = saveTemperatureIntervalInMin * 2;

                    Console.WriteLine($"TemperatureSaveInterval is {saveTemperatureIntervalInMin} and check time is {checkTime}");

                    var dateTimeFromDB = Helper.GetSettingFromDb(conn, "RestartTime");

                    if (!DateTime.TryParseExact(dateTimeFromDB, format, provider, DateTimeStyles.None, out DateTime lastRestart))
                    {
                        Console.WriteLine("Failed to parse the RestartTime value from the database.");
                        lastRestart = DateTime.Now;
                    }
                    else
                    {
                        Console.WriteLine($"Last restart was at {lastRestart.ToString(format, provider)}");
                    }

                    while (!Console.KeyAvailable)
                    {
                        try
                        {
                            string lastDate = Helper.GetLastSettingValue(conn);

                            if (DateTime.TryParse(lastDate, out DateTime lastDateDateTime))
                            {
                                // Check if the specified checkTime minutes have passed since the last date
                                if (lastDateDateTime.AddMinutes(checkTime) < DateTime.Now)
                                {
                                    Helper.SaveSettingValue(conn, "RestartTime", DateTime.Now.ToString(format, CultureInfo.InvariantCulture));
                                    RestartMachine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Failed to parse the lastDate value.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Got an error: {ex.Message} StackTrace: {ex.StackTrace}");

                            if (ex.InnerException != null)
                            {
                                Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Error InnerException: {ex.InnerException.Message}");
                            }

                            // Attempt to reconnect if the connection is lost
                            if (ex is MySqlException || ex is InvalidOperationException)
                            {
                                Console.WriteLine("Attempting to reconnect to the database...");
                                while (true)
                                {
                                    try
                                    {
                                        conn.Close(); // Ensure the previous connection is closed
                                        conn.Open();
                                        Console.WriteLine("Reconnected to the database.");
                                        break; // Exit the reconnect loop
                                    }
                                    catch (Exception reconnectEx)
                                    {
                                        Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} Reconnection failed: {reconnectEx.Message}");
                                        Console.WriteLine("Retrying in 1 minute...");
                                        Thread.Sleep(TimeSpan.FromMinutes(1));
                                    }
                                }
                            }
                        }
                        finally
                        {
                            Thread.Sleep(1000); // Sleep for 1 second before the next check
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk"))} An unexpected error occurred: {ex.Message}");
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
                    // Optionally, wait for the process to exit
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
