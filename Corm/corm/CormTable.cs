using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Xml.Serialization;
using Corm.attrs;
using Corm.utils;

namespace Corm
{
    public class CormTable<T> where T : new()
    {
        public Corm _corm { get; }
        public string _tableName { get; }
        private List<string> columnNameTemp = new List<string>();
        public List<string> ColumnNameTemp => columnNameTemp;
        
        public CormTable(Corm corm)
        {
            this._corm = corm;
            var properties = typeof(T).GetProperties();
            // 自动分析 TableName
            var tableAttributes = typeof(T).GetCustomAttributes(typeof(CormTable), true);
            if (tableAttributes.Length > 0)
            {
                CormTable tableAttr = tableAttributes[0] as CormTable;
                if (tableAttr != null && tableAttr.TableName != null && !tableAttr.TableName.Trim().Equals(""))
                {
                    this._tableName = tableAttr.TableName;
                }
            }
            if (this._tableName == null || this._tableName.Trim().Equals(""))
            {
                throw new CormException("Entity 类 " + typeof(T).Name + "没有指定 TableName 属性，" +
                                        "请使用 [CormTable(TableName=\"xxx\")] 或在CormTable 构造函数中指定");
   
            }
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

        public CormDeleteMiddleSql<T> Delete()
        {
            return new CormDeleteMiddleSql<T>(this);
        }
        
        public SqlCommand SqlCommand(string sql)
        {
            return new SqlCommand(sql, this._corm._sqlConnection);
        }
        
    }
    
    
}