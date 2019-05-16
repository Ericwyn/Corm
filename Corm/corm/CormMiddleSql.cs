using System;
using System.Collections.Generic;
using System.Data;
using Corm.attrs;

namespace Corm
{
    // 用来保存查询语句的中间状态
    public class CormSelectMiddleSql<T>
    {
        private CormTable<T> _cormTable;
        private string sqlBuff;
        // 查找的属性
        private string attributes = "*";
        private string tableName;

        public CormSelectMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
        }

        // 查询全部
        public CormSelectMiddleSql<T> All()
        {
            this.attributes = "*";
            return this;
        }

        public CormSelectMiddleSql<T> Attributes()
        {
            return this;
        }
        
        public List<T> Commit()
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var objAttrs = property.GetCustomAttributes(typeof(CormColumn), true);
                if (objAttrs.Length > 0)
                {
                    CormColumn attr = objAttrs[0] as CormColumn;
                    if (attr != null)
                    {
                        Console.WriteLine(attr.Name);
                        Console.WriteLine(attr.Length);
                        Console.WriteLine(attr.DbType.ToString());
                    }
                }
                
            }
//            this._cormTable._corm._sqlConnection.            
            return null;
        }

    }
}