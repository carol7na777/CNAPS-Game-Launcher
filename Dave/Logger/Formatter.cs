using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Dave.Logger
{
    public class Formatter
    {
        public static string Format(LogLevel logLevel, string message)
        {
            string time = GetCurrentTime();

            StringBuilder stringBuilderNoColor = new();
            stringBuilderNoColor.Append("[" + time + "] ");
            stringBuilderNoColor.Append("[" + LogLevelToString(logLevel) + "] ");
            stringBuilderNoColor.AppendLine(message);

            return stringBuilderNoColor.ToString();
        }

        private static string GetCurrentTime()
        {
            return DateTime.Now.ToString();
        }

        private static string LogLevelToString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.DEBUG:
                    return "DEBUG";
                case LogLevel.INFO:
                    return "INFO";
                case LogLevel.WARNING:
                    return "WARNING";
                case LogLevel.ERROR:
                    return "ERROR";
                case LogLevel.CRITICAL:
                    return "CRITICAL";
            }
            return "UNKNOWN";
        }
    }
}
