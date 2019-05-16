using System;

namespace Corm.utils
{
    public class CormLog
    {
        public static void ConsoleLog(string logMsg)
        {
            Console.WriteLine("[Corm Log Start] --------------------------- ");
            Console.WriteLine(logMsg);
            Console.WriteLine("-------------------------------------------- ");
        }
    }
}