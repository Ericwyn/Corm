using System;
using System.Data;

namespace CORM.attrs
{
    [AttributeUsage(AttributeTargets.Field |
                    AttributeTargets.Property,
        AllowMultiple = false)]
    public class Column : System.Attribute
    {
        // 字段名称
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        // 数据大小
        private int size ;

        public int Size
        {
            get { return size; }
            set { size = value; }
        }
        
        // 数据类型
        private SqlDbType dbType;

        public SqlDbType DbType
        {
            get { return dbType; }
            set { dbType = value; }
        }
        
        
        // 主键
        private bool primaryKey = false;

        public bool PrimaryKey
        {
            get { return primaryKey; }
            set
            {
                // 如果是 PrimaryKey 的话，那么一定是 notNull
                if (value)
                {
                    this.notNull = true;
                }
                primaryKey = value;
            }
        }
        
        // 是否非空
        private bool notNull = false;

        public bool NotNull
        {
            get { return notNull; }
            set { notNull = value; }
        }
    }
}