using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using CORM.attrs;
using CORM.utils;

namespace CORM
{
    public class CormTable<T> where T : new()
    {
        public Corm _corm { get; }
        public string _tableName { get; }
        public Dictionary<string, PropertyInfo> PropertyMap { get; }
        
        public CormTable(Corm corm)
        {
            this._corm = corm;
            var properties = typeof(T).GetProperties();
            // 自动分析 TableName
            var tableAttributes = typeof(T).GetCustomAttributes(typeof(Table), true);
            if (tableAttributes.Length > 0)
            {
                Table tableAttr = tableAttributes[0] as Table;
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

            PropertyMap = new Dictionary<string, PropertyInfo>();
            foreach (var property in properties)
            {
                var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                if (objAttrs.Length > 0)
                {
                    Column attr = objAttrs[0] as Column;
                    if (attr != null)
                    {
                        PropertyMap.Add(attr.Name, property);
                        continue;
                    }
                }
                PropertyMap.Add(property.Name, property);
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
        
        // Delete 删除操作
        public CormDeleteMiddleSql<T> Delete()
        {
            return new CormDeleteMiddleSql<T>(this);
        }
        
        // 自定义 SQL 查询语句操作
        public CormCustomizeMiddleSql<T> Customize()
        {
            return new CormCustomizeMiddleSql<T>(this);
        }
        
        public CormTransaction BeginTransaction()
        {
            return new CormTransaction(this._corm);
        }

        public void SqlLog(string sql)
        {
            _corm.LogUtils.SqlPrint(sql);
        }

        // 获取数据库定义
        public string DDL()
        {
            StringBuilder ddl = new StringBuilder("CREATE TABLE dbo.").Append(_tableName).Append(" ").Append("(").Append(Environment.NewLine);
            int count = 1;
            foreach (string columnName in PropertyMap.Keys)
            {
                Column attr = PropertyMap[columnName].GetCustomAttributes(typeof(Column), true)[0] as Column;
                ddl.Append(attr.Name).Append(" ").Append(attr.DbType.ToString()).Append(" ");
                if (attr.Size != null)
                {
                    ddl.Append("(").Append(attr.Size).Append(")").Append(" ");
                }

                if (attr.DbType.ToString().ToLower().Contains("char"))
                {
                    ddl.Append("COLLATE Chinese_PRC_CI_AS").Append(" ");
                }

                ddl.Append("NULL");
                if (count < PropertyMap.Count)
                {
                    ddl.Append(",");
                }

                ddl.Append(Environment.NewLine);
                count++;
            }

            return ddl.Append(") GO").ToString();
        }
        
        
    }
    
    
}