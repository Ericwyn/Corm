using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using CORM.attrs;
using CORM.utils;

namespace CORM
{
    /*
     * 用来保存 Update 中间状态的 sql
     *
     * 更新操作需要使用 Where() 指定 whereObj 用来限定更新的 规则
     * 而使用 Value() 来指定需要更新成什么样的数据
     * 
     */
    public class CormUpdateMiddleSql<T> where T : new()
    {
        private CormTable<T> _cormTable;
        private StringBuilder sqlBuilder = new StringBuilder("");
        private string tableName;
        // 缓存该类型的列名，避免经常反射
        private List<string> columnNameTemp;

        // 用来设置更新列的过滤规则
        private T whereObj;
        // 设置更新成什么样的数据
        private T updateObj;
        
        public CormUpdateMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
//            this.columnNameTemp = cormTable.ColumnNameTemp;
        }

        public CormUpdateMiddleSql<T> Where(T obj)
        {
            this.whereObj = obj;
            return this;
        }

        public CormUpdateMiddleSql<T> Value(T obj)
        {
            this.updateObj = obj;
            return this;
        }

        public int Commit()
        {
            return Commit(null);
        }
        
        public int Commit(CormTransaction transaction)
        {
            int resUpdateSize = -1;
            if (updateObj == null || whereObj == null)
            {
                throw new Exception("Update 操作需要指定 Update 的条件以及替换的 Value，请同时调用 Where() 和 Value() 方法");
            }
            var valueQuery = GetValueQuery(updateObj);
            var whereQuery = GetWhereQuery(whereObj);
            if (valueQuery.Trim().Equals("") || whereQuery.Trim().Equals(""))
            {
                throw new Exception("WHERE 条件或 SET 条件为空");
            }
            // "UPDATE " + this.tableName + " "+"\n" + GetValueQuery(updateObj) + " " + GetWhereQuery(whereObj) +" ;"
            sqlBuilder.Append("UPDATE ");
            sqlBuilder.Append(this.tableName);
            sqlBuilder.Append(" \n");
            sqlBuilder.Append(GetValueQuery(updateObj));
            sqlBuilder.Append(" ");
            sqlBuilder.Append(GetWhereQuery(whereObj));
            sqlBuilder.Append(" ;");

            var sql = sqlBuilder.ToString();
            List<SqlParameter> paramList = new List<SqlParameter>(); 
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                // 从注解拿到具体的字段名称，拼接
                var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                if (objAttrs.Length > 0)
                {
                    Column attr = objAttrs[0] as Column;
                    if (attr == null)
                    {
                        continue;
                    }
                    if (sql.Contains(attr.Name + flagForOldValue + " "))
                    {
                        var param = new SqlParameter();
                        // 创建 param 以填充 sqlBuff 当中的占位符
                        param = new SqlParameter("@" + attr.Name + flagForOldValue, attr.DbType, attr.Size);
                        var value = property.GetValue(whereObj);
                        // 如果这个属性存在的话
                        if (value != null)
                        {
                            param.Value = value;
                        }
                        else
                        {
                            throw new CormException("UPDATE 操作当中，WHERE 语句拼接错误");
                        }
                        paramList.Add(param);
                    }

                    if (sql.Contains(attr.Name + flagForValueQuery + " "))
                    {
                        var param = new SqlParameter();
                        // 创建 param 以填充 sqlBuff 当中的占位符
                        param = new SqlParameter("@" + attr.Name + flagForValueQuery, attr.DbType, attr.Size);
                        var value = property.GetValue(updateObj);
                        // 如果这个属性存在的话
                        if (value != null)
                        {
                            param.Value = value;
                        }
                        else
                        {
                            throw new CormException("UPDATE 操作当中, SET 语句拼接错误");
                        }
                        paramList.Add(param);
                    }
                }
            }
            
            this._cormTable.SqlLog(sql);
            if (transaction != null)
            {
                resUpdateSize = transaction.AddSql(sql, paramList).ExecuteNonQuery();
            }
            else
            {
                using (SqlConnection conn = this._cormTable._corm.NewConnection())
                {
                    var sqlCommand = new SqlCommand(sql, conn);
                    foreach (SqlParameter param in paramList)
                    {
                        sqlCommand.Parameters.Add(param);
                    }
                    resUpdateSize = sqlCommand.ExecuteNonQuery();
                }
            }
            if (resUpdateSize < 0)
            {
                throw new CormException(" UPDATE 操作，受影响操作函数 < 0，请检查是否有错误");
            }
            return resUpdateSize;
        }
        
        /*
         * 得到 where 语句 <column_name> = @<column_name>OLD
         */
        private static string flagForOldValue = "OLD";
        private static string GetWhereQuery(T obj)
        {
            if (obj == null){
                return "";
            }
            var resWhereQuery = "";
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                // 如果这个属性存在的话
                if (value != null)
                {
                    // 从注解厘米拿到具体的字段名称，拼接
                    var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        if (attr != null)
                        {
                            resWhereQuery += " " + attr.Name + "=@" + attr.Name + flagForOldValue + " and";
                        }
                    }
                }
            }
            if (resWhereQuery.EndsWith("and"))
            {
                resWhereQuery = "WHERE "+resWhereQuery.Substring(0, resWhereQuery.Length - 3);
            }
            return resWhereQuery;
        }
        
        /*
         * 得到 update 语句，  <column_name> = @<column_name>VALUE
         */
        private static string flagForValueQuery = "VALUE";
        private static string GetValueQuery(T obj)
        {
            if (obj == null){
                return "";
            }
            var resWhereQuery = "";
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                // 如果这个属性存在的话
                if (value != null)
                {
                    // 从注解厘米拿到具体的字段名称，拼接
                    var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        if (attr != null)
                        {
                            resWhereQuery += " " + attr.Name + "=@" + attr.Name + flagForValueQuery + " ,";
                        }
                    }
                }
            }
            if (resWhereQuery.EndsWith(","))
            {
                resWhereQuery = "SET " + resWhereQuery.Substring(0, resWhereQuery.Length - 1);
            }
            return resWhereQuery;
        }
    }
}