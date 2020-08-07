using System;

namespace AquariumController.Extension
{
    public static class ConsoleEx
    {
        public static void WriteLineWithDate(string value)
        {
            Console.WriteLine(DateTime.Now + " " + value);
        }
    }
}
