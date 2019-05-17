using System;

namespace CORM.attrs
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