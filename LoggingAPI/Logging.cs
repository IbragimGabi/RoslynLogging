using System;
using System.IO;

namespace LoggingAPI
{
    public static class Logging
    {

        public static void LogToFile(string info)
        {
            File.AppendAllText(".\\Logging.txt", info);
        }

        public static void LogToConsole(string info)
        {
            Console.WriteLine(info);
        }

    }
}
