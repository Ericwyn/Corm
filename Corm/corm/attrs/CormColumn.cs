using System;
using System.Data;

namespace Corm.attrs
{
    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Constructor |
                    AttributeTargets.Field |
                    AttributeTargets.Method |
                    AttributeTargets.Property,
        AllowMultiple = false)]
    public class CormColumn : System.Attribute
    {
        private string name;

        public string Name
        {
            get => name;
            set => name = value;
        }

        private int _size ;

        public int Size
        {
            get => _size;
            set => _size = value;
        }

        private SqlDbType dbType;

        public SqlDbType DbType
        {
            get => dbType;
            set => dbType = value;
        }
    }
}