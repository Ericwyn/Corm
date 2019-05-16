using System;

namespace Corm.attrs
{
    [AttributeUsage(AttributeTargets.Class ,
            AllowMultiple = false)]
    public class CormTable : System.Attribute
    {
        
    }
}