using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public static class Log
    {
        public static void ConsoleWriteLine(string message)
        {
            Console.WriteLine($"===== {message} =====");
        }
        public static void DebugWriteLine(string message)
        {
            Debug.WriteLine($"===== {message} =====");
        }
        public static void ConsoleAndDebugWriteLine(string message)
        {
            ConsoleWriteLine(message);
            DebugWriteLine(message);
        }
    }
}
