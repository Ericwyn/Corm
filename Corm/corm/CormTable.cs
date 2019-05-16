using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Xml.Serialization;
using Corm.attrs;

namespace Corm
{
    public class CormTable<T> where T : new()
    {
        public Corm _corm { get; }
        public string _tableName { get; }
        private List<string> columnNameTemp = new List<string>();
        public List<string> ColumnNameTemp => columnNameTemp;

        public CormTable(Corm corm ,string tableName)
        {
            this._corm = corm;
            this._tableName = tableName;
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var objAttrs = property.GetCustomAttributes(typeof(CormColumn), true);
                if (objAttrs.Length > 0)
                {
                    CormColumn attr = objAttrs[0] as CormColumn;
                    if (attr != null)
                    {
                        this.columnNameTemp.Add(attr.Name);
                    }
                }
            }
        }

        // Select 查询
        public CormSelectMiddleSql<T> Find()
        {
            return new CormSelectMiddleSql<T>(this);
        }

        // Inset 插入
        public CormInsertMiddleSql<T> Insert()
        {
            return new CormInsertMiddleSql<T>(this);
        }
        
        // Update 更新
        public CormUpdateMiddleSql<T> Update()
        {
            return new CormUpdateMiddleSql<T>(this);
        }
        
        public SqlCommand SqlCommand(string sql)
        {
            return new SqlCommand(sql, this._corm._sqlConnection);
        }
        
    }
    
    
}