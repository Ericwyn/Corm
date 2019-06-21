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
        
        // 自定义 WhereQuery 查询语句
        private string cusWhereQuery = null;
        // 自定义 WhereQuery 查询的参数
        private SqlParameter[] cusWhereQueryParams; 
        
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
        
        public CormUpdateMiddleSql<T> WhereQuery(string query)
        {
            if (cusWhereQuery != null && !cusWhereQuery.Equals(""))
            {
                throw new CormException("UPDATE 操作中, Where() 方法和 WhereQuery() 方法不可同时使用");
            }
            return WhereQuery(query, null);
        }
        
        // 自定义 Where 查询
        // 该方法不能和 Where 方法同时使用
        // query 是自定义的 where 语句，例如  "and a > b" , "or a < 10" 之类的
        // parameters 是自定义 where 语句的参数
        public CormUpdateMiddleSql<T> WhereQuery(string query, SqlParameter[] parameters)
        {
            if (whereObj != null)
            {
                throw new CormException("UPDATE 操作中, Where() 方法和 WhereQuery() 方法不可同时使用");
            }
            if (parameters != null && parameters.Length > 0)
            {
                cusWhereQueryParams = parameters;
            }

            if (query != null && !query.Equals(""))
            {
                cusWhereQuery = query;
            }
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
            if (updateObj == null || (whereObj == null && cusWhereQuery == null))
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
            if (whereObj != null)
            {
                sqlBuilder.Append(GetWhereQuery(whereObj));
            } else if (cusWhereQuery != null)
            {
                sqlBuilder.Append(" WHERE ").Append(cusWhereQuery);
            }
            sqlBuilder.Append(" ;");

            var sql = sqlBuilder.ToString();
            this._cormTable.SqlLog(sql);
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
                    
                    // 如果是以 Where 方法来设定更新条件的话
                    if (whereObj != null)
                    {
                        // 拼接旧的参数， Where 语句
                        if (sql.Contains("@" + attr.Name + flagForOldValue +" "))
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
                                throw new CormException("UPDATE 操作当中，WHERE 语句拼接错误 --> 参数名:" + attr.Name);
                            }
                            paramList.Add(param);
                        }
                    }
                    // 拼接新的参数， Value 语句
                    if (sql.Contains("@" + attr.Name + flagForValueQuery +" "))
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
                            throw new CormException("UPDATE 操作当中, SET 语句拼接错误 --> 参数名:" + attr.Name);
                        }
                        paramList.Add(param);
                    }
                }
            }

            // 如果是以 WhereQuery 方法来设定更新条件的话
            if (cusWhereQueryParams != null && cusWhereQueryParams.Length > 0)
            {
                foreach (SqlParameter parameter in cusWhereQueryParams)
                {
                    paramList.Add(parameter);
                }
            }
            
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
         * 得到 where 语句 <column_name> = @<column_name>_OLD_
         */
        private static string flagForOldValue = "_OLD_";
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
         * 得到 update 语句，  <column_name> = @<column_name>_VALUE_
         */
        private static string flagForValueQuery = "_VALUE_";
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