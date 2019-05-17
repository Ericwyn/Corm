using System;

namespace Corm.attrs
{
    [AttributeUsage(AttributeTargets.Class ,
            AllowMultiple = false)]
    public class CormTable : System.Attribute
    {
        private string tableName;

        public string TableName
        {
            get => tableName;
            set => tableName = value;
        }
    }
}