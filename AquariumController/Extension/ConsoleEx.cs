using System;
using System.Globalization;

namespace AquariumController.Extension
{
    public static class ConsoleEx
    {
        public static void WriteLineWithDate(string value)
        {
            Console.WriteLine(DateTime.Now.ToString(CultureInfo.CreateSpecificCulture("da-dk")) + " " + value);
        }
    }
}
