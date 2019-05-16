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

        private int length ;

        public int Length
        {
            get => length;
            set => length = value;
        }

        private SqlDbType dbType;

        public SqlDbType DbType
        {
            get => dbType;
            set => dbType = value;
        }
    }
}