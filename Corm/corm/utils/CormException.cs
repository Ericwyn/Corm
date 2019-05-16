using System;

namespace Corm.utils
{
    public class CormException : Exception
    {
        public CormException(string message) : base(message)
        {
            
        }
 
        public override string Message
        {
            get
            {
                return "[CORM 异常]" + base.Message;
            }
        }
    }
}