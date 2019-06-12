using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Security;
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
            // 自动分析 TableName
            this._tableName = CormUtils<T>.GetTableName();
            if (this._tableName == null || this._tableName.Trim().Equals(""))
            {
                throw new CormException("Entity 类 " + typeof(T).Name + "没有指定 TableName 属性，" +
                                        "请使用 [CormTable(TableName=\"xxx\")] 或在CormTable 构造函数中指定");
            }
            PropertyMap = CormUtils<T>.GetProPropertyInfoMap();
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
            List<string> primaryKeySQL = new List<string>();
            foreach (string columnName in PropertyMap.Keys)
            {
                Column attr = PropertyMap[columnName].GetCustomAttributes(typeof(Column), true)[0] as Column;
                ddl.Append("    ").Append(attr.Name).Append(" ").Append(attr.DbType.ToString());
                if (attr.Size != null && attr.Size > 0)
                {
                    ddl.Append("(").Append(attr.Size).Append(")");
                }
                
                if (attr.DbType.ToString().ToLower().Contains("char"))
                {
                    ddl.Append(" COLLATE Chinese_PRC_CI_AS");
                }

                ddl.Append(" ");
                
                if (attr.NotNull)
                {
                    ddl.Append("NOT NULL");
                }
                else
                {
                    ddl.Append("NULL");
                }
                ddl.Append(",");
                ddl.Append(Environment.NewLine);

                if (attr.PrimaryKey)
                {
                    primaryKeySQL.Add("CONSTRAINT " + _tableName + "_PK PRIMARY KEY (" + attr.Name + ")");
                }
            }

            var primaryKeyCount = primaryKeySQL.Count;
            for (int i = 0; i < primaryKeyCount; i++)
            {
                ddl.Append("    ").Append(primaryKeySQL[i]).Append(",");
                ddl.Append(Environment.NewLine);
            }
            
            return ddl.Append(") GO").ToString();
        }
        
        
    }
    
    
}