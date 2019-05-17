using System;
using System.Data;

namespace CORM.attrs
{
    [AttributeUsage(AttributeTargets.Field |
                    AttributeTargets.Property,
        AllowMultiple = false)]
    public class CormColumn : System.Attribute
    {
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private int _size ;

        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        private SqlDbType dbType;

        public SqlDbType DbType
        {
            get { return dbType; }
            set { dbType = value; }
        }
    }
}