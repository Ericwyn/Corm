using System;

namespace Corm.utils
{
    public class CormLog
    {
        public static void ConsoleLog(string logMsg)
        {
            Console.WriteLine("\n[Corm Log] -------------------------------");
            Console.WriteLine(logMsg);
            Console.WriteLine("-------------------------------------------- \n");
        }
    }
}