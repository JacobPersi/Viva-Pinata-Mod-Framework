using System;

namespace PinataParty.Bootstrap {

    public static class ConsoleEx {

        public static void SystemPrint(string message) {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[System] ");
            Console.ForegroundColor = oldColor;
            Console.Write(message);
        }

        public static void ReportSuccess() {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK]");
            Console.ForegroundColor = oldColor;
        }

        public static void ReportFailure() {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[FAIL]");
            Console.ForegroundColor = oldColor;
        }
    }
}
