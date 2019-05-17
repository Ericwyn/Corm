using System;

namespace CORM.utils
{
    public class CormException : Exception
    {
        public CormException(string message) : base("[CORM 异常] " + message)
        {
           
        }

    }
}