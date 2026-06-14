using AquariumController.Extension;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using UnitsNet;

namespace AquariumController.Helper
{
    public static class Tempertur
    {

        // Base path where the device directories are located
        static string basePath = "/sys/bus/w1/devices/";

        public const double TemperturCalibrateOffSet = 0;

        public static double TemperturValue = 0;
        public static double TemperatureMin = 0;
        public static double TemperatureMax = 0;
        public static void SaveTempertur(Object stateInfo)
        {
            if (TemperturValue > 0)
            {
                ConsoleEx.WriteLineWithDate($"Save tempertur with value {TemperturValue}");

                MySqlConnection localConn = new MySqlConnection(ConfigurationManager.AppSettings.Get("ConnectionString"));
                localConn.Open();

                Helpers.DB.Helper.SaveChannelValue(localConn, "temperature", TemperturValue);

                localConn.Close();
                localConn.Dispose();
            }
            else
            {
                ConsoleEx.WriteLineWithDate($"Do not save tempertur if it is 0");
            }

        }

        public static void SetupMaxMinTemperature(MySqlConnection conn)
        {
            int maxTmp = int.Parse(Helpers.DB.Helper.GetSettingFromDb(conn, "TemperatureMax"));
            int minTmp = int.Parse(Helpers.DB.Helper.GetSettingFromDb(conn, "TemperatureMin"));
            if ((maxTmp != TemperatureMax) || (minTmp != TemperatureMin))
            {
                TemperatureMax = maxTmp;
                ConsoleEx.WriteLineWithDate($"TemperatureMax is {TemperatureMax}");

                TemperatureMin = minTmp;
                ConsoleEx.WriteLineWithDate($"TemperatureMin is {TemperatureMin}");
            }
                
        }

        // Get all directories in the base path that start with "28"
        public static IEnumerable<string> GetDirectories()
        {
            IEnumerable<string> directories = null;
            try
            {
                // Get all directories in the base path that start with "28"
                directories = Directory.GetDirectories(basePath)
                                          .Where(dir => Path.GetFileName(dir).StartsWith("28"));

                if (!directories.Any())
                {
                    Console.WriteLine("No device directories starting with '28' were found.");
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

            return directories;
        }

        public static List<double> ReadTemperatur(IEnumerable<string> directories)
        {
            List<double> list = new List<double>();

            try
            {
                // Iterate through each directory
                foreach (var dir in directories)
                {
                    string deviceName = Path.GetFileName(dir);
                    string tempFilePath = Path.Combine(dir, "temperature");

                    // Check if the temperature file exists
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            // Read the temperature value as a string
                            string tempString = File.ReadAllText(tempFilePath).Trim();

                            if (string.IsNullOrEmpty(tempString)) {
                                Thread.Sleep(10000); // waite for 10 sec
                                tempString  = File.ReadAllText(tempFilePath).Trim();
                            }

                            // Parse the string to an integer
                            if (int.TryParse(tempString, out int tempMilli))
                            {
                                // Convert to double by dividing by 1000.0
                                double temperature = (tempMilli / 1000.0) - TemperturCalibrateOffSet;

                                list.Add(temperature);

                            }
                            else
                            {
                                Console.WriteLine($"[Warning] Unable to parse temperature value '{tempString}' in {tempFilePath}.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Error] Failed to read temperature from {tempFilePath}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Warning] Temperature file not found in directory {dir}.");
                        Console.WriteLine($"Trying to find new directory!");
                        GetDirectories();
                    }
                }
            }

           
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] An unexpected error occurred: {ex.Message}");
            }

            return list;
        }

    }
}
