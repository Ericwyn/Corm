using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Security;
using System.Text;
using CORM.attrs;
using CORM.utils;

namespace CORM
{
    /**
     * 一个结构体，用来存储表结构中，一个字段的信息
     */
    public class TableColumnStruct
    {
        public string ColumnName { get; set; }
        public SqlDbType DbType { get; set; }
        public int? Size { get; set; }
        public bool NotNull { get; set; }
    }
    
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
        
        // 删除该表
        public void DropTable()
        {
            if (!Exist()){return;}
            using (SqlConnection conn = _corm.NewConnection())
            {
                var sql = @"DROP TABLE " + _tableName;
                this.SqlLog(sql);
                SqlCommand sqlCommand = new SqlCommand(sql, conn);
                var count = sqlCommand.ExecuteNonQuery();
            }
        }
        
        // 创建表格
        public void CreateTable()
        {
            var sql = DDL();
            using (SqlConnection conn = _corm.NewConnection())
            {
                this.SqlLog(sql);
                SqlCommand sqlCommand = new SqlCommand(sql, conn);
                sqlCommand.ExecuteNonQuery(); 
            }
        }

        public bool Exist()
        {
            using (SqlConnection conn = _corm.NewConnection())
            {
                var sql = @"select TABLE_NAME from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='"+_tableName+"';" ;
                this.SqlLog(sql);
                SqlCommand sqlCommand = new SqlCommand(sql, conn);
                using (var reader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return true;
                    }
                    else
                    {
                        reader.Close();
                        return false;
                    }
                }
            }
        }
        
        public CormTransaction BeginTransaction()
        {
            return new CormTransaction(this._corm);
        }

        public void SqlLog(string sql)
        {
            _corm.LogUtils.SqlPrint(sql);
        }
        
        // 获取当前数据表的表结构
        public List<TableColumnStruct> GetTableStruct()
        {
            var columnList = new List<TableColumnStruct>();
            using (SqlConnection conn = _corm.NewConnection())
            {
                var sql = @"select 
                                COLUMN_NAME,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,IS_NULLABLE  
                                from INFORMATION_SCHEMA.COLUMNS t where t.TABLE_NAME = '" + _tableName + "'";
                SqlCommand sqlCommand = new SqlCommand(sql, conn);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["CHARACTER_MAXIMUM_LENGTH"] != null && !reader["CHARACTER_MAXIMUM_LENGTH"].ToString().Equals(""))
                        {
                            columnList.Add(new TableColumnStruct()
                            {
                                ColumnName = reader["COLUMN_NAME"].ToString(),
                                DbType = CormUtils<Corm>.parseDbType(reader["DATA_TYPE"].ToString()),
                                Size = int.Parse(reader["CHARACTER_MAXIMUM_LENGTH"].ToString()),
                                NotNull = reader["IS_NULLABLE"].ToString().Equals("NO")
                            }); 
                        }
                        else
                        {
                            columnList.Add(new TableColumnStruct()
                            {
                                ColumnName = reader["COLUMN_NAME"].ToString(),
                                DbType = CormUtils<Corm>.parseDbType(reader["DATA_TYPE"].ToString()),
                                NotNull = reader["IS_NULLABLE"].ToString().Equals("NO")
                            });
                        }
                    }
                }
            }

            return columnList;
        }

        // 同步表结构
        public void SyncTableStruct()
        {
            List<TableColumnStruct> oldStructs = GetTableStruct();
            var oldStructsColumnNameMap = new HashSet<string>();
            foreach (TableColumnStruct oldStruct in oldStructs)
            {
                oldStructsColumnNameMap.Add(oldStruct.ColumnName);
            }
            foreach (string newStructColumnName in PropertyMap.Keys)
            {
                // 新的字段名不存在与旧的表那里
                if (!oldStructsColumnNameMap.Contains(newStructColumnName))
                {
                    AddColumn(PropertyMap[newStructColumnName]);
                }
            }
            
            
        }

        // 在已有的表上面，添加一个新的字段
        private void AddColumn(PropertyInfo propertyInfo)
        {
            using (SqlConnection conn = _corm.NewConnection())
            {
                Column attr = propertyInfo.GetCustomAttributes(typeof(Column), true)[0] as Column;
                StringBuilder sqlBuilder = new StringBuilder("ALTER TABLE ").Append(_tableName).Append(" ADD ");
                sqlBuilder.Append(attr.Name).Append(" ").Append(attr.DbType.ToString()).Append(" ");
                if (attr.NotNull)
                {
                    sqlBuilder.Append("NOT NULL");
                }
                else
                {
                    sqlBuilder.Append("NULL");
                }
                sqlBuilder.Append(" ;");   
                SqlLog("表 " + _tableName +" 添加新字段 " + attr.Name +"\n" + sqlBuilder);
                SqlCommand sqlCommand = new SqlCommand(sqlBuilder.ToString(), conn);
                sqlCommand.ExecuteNonQuery();
            }
        }
        
        // 获取建表语句
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
            
            return ddl.Append(")").ToString();
        }
        
        
    }
    
    
}