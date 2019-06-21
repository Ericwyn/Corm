using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using CORM.attrs;
using CORM.utils;

namespace CORM
{
    public class CormDeleteMiddleSql<T> where T : new()
    {
        private CormTable<T> _cormTable;
        private StringBuilder sqlBuilder = new StringBuilder("");
        private string tableName;
        // 缓存该类型的列名，避免经常反射
        private Dictionary<string, PropertyInfo> PropertyMap;

        private T whereObj;
        private bool deleteAllFlag = false;
        
        // 自定义 WhereQuery 查询语句
        private string cusWhereQuery = null;
        // 自定义 WhereQuery 查询的参数
        private SqlParameter[] cusWhereQueryParams; 
        
        public CormDeleteMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
            this.PropertyMap = _cormTable.PropertyMap;
        }

        public CormDeleteMiddleSql<T> Where(T obj)
        {
            
            if (cusWhereQuery != null && !cusWhereQuery.Equals(""))
            {
                throw new CormException("DELETE 操作中, Where() 方法和 WhereQuery() 方法不可同时使用");
            }
            this.whereObj = obj;
            return this;
        }
        
        public CormDeleteMiddleSql<T> WhereQuery(string query)
        {
            return WhereQuery(query, null);
        }
        
        // 自定义 Where 查询
        // 该方法不能和 Where 方法同时使用
        // query 是自定义的 where 语句，例如  "and a > b" , "or a < 10" 之类的
        // parameters 是自定义 where 语句的参数
        public CormDeleteMiddleSql<T> WhereQuery(string query, SqlParameter[] parameters)
        {
            if (whereObj != null)
            {
                throw new CormException("DELETE 操作中, Where() 方法和 WhereQuery() 方法不可同时使用");
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
        
        public CormDeleteMiddleSql<T> All()
        {
            deleteAllFlag = true;
            return this;
        }

        public int Commit()
        {
            return Commit(null);
        }
        
        /*
         * DELETE FROM [dbo].[Product]
         */
        public int Commit(CormTransaction transaction)
        {
            int resDeleteSize = -1;
            sqlBuilder.Append("DELETE FROM ");
            sqlBuilder.Append(this.tableName);
            sqlBuilder.Append(" ");

            // 既没有使用 All() 方法也没有使用 Where() 方法 也没有使用 WhereQuery 方法，抛出异常提示用户
            if (whereObj == null && !deleteAllFlag && (cusWhereQuery == null || cusWhereQuery.Trim().Equals("")))
            {
                throw new CormException("DELETE 需要指定删除范围，请使用 All() 方法或者 Where() 方法");
            }
            else
            {
                // 先判断 All 条件, all flag 为 true ，另外两个方法未被调用
                if (whereObj == null && deleteAllFlag && cusWhereQuery == null)
                {
                    // All 条件
                    sqlBuilder.Append(" ;");
                }
                // 只调用了 whereObj 方法
                else if (whereObj != null && !deleteAllFlag && (cusWhereQuery == null || cusWhereQuery.Trim().Equals("")))
                {
                    // Query 条件
                    var whereQuery = GetWhereQuery(whereObj);
                    if (whereQuery.Trim().Equals(""))
                    {
                        throw new CormException("DELETE 没有指定具体的 Where 条件");
                    }
                    else
                    {
                        sqlBuilder.Append(whereQuery);
                        sqlBuilder.Append(" ;");
                    }
                }
                // 只调用了 WhereQuery 方法
                else if (cusWhereQuery != null && !cusWhereQuery.Trim().Equals("") && whereObj == null && !deleteAllFlag)
                {
                    sqlBuilder.Append(" WHERE ").Append(cusWhereQuery).Append(" ;");
                }
                else
                {
                    throw new CormException("DELETE 操作时候，请仅使用 Where/ WhereQuery/ All 方法中的一个，来限定删除范围");
                }
            }
            
            var sql = sqlBuilder.ToString();
            this._cormTable.SqlLog(sql);            
            List<SqlParameter> paramList = new List<SqlParameter>(); 
            var properties = typeof(T).GetProperties();
            if (whereObj != null && sql.Contains("WHERE "))
            {
                foreach (string key in PropertyMap.Keys)
                {
                    if (sql.Contains("@"+key+" "))
                    {
                        // 证明预编译语句里面有这个属性
                        var value = PropertyMap[key].GetValue(this.whereObj);
                        Column attr = PropertyMap[key].GetCustomAttributes(typeof(Column), true)[0] as Column;
                        var param = new SqlParameter("@"+attr.Name, attr.DbType, attr.Size);
                        param.Value = value;
                        paramList.Add(param);
                    }
                }
            } else if (cusWhereQueryParams != null && cusWhereQueryParams.Length != 0)
            {
                paramList.AddRange(cusWhereQueryParams);
            }

            if (transaction != null)
            {
                resDeleteSize = transaction.AddSql(sql, paramList).ExecuteNonQuery();
            }
            else
            {
                using (SqlConnection conn = this._cormTable._corm.NewConnection())
                {
                    SqlCommand sqlCommand = new SqlCommand(sql, conn);
                    foreach (SqlParameter param in paramList)
                    {
                        sqlCommand.Parameters.Add(param);
                    }
                    resDeleteSize = sqlCommand.ExecuteNonQuery();
                }
            }
            if (resDeleteSize < 0)
            {
                throw new CormException(" DELETE 操作，受影响操作函数 < 0，请检查是否有错误");
            }
            return resDeleteSize;
        }
        
        
        private string GetWhereQuery(T obj)
        {
            if (obj == null){
                return "";
            }
            var resWhereQuery = new StringBuilder("");
            var properties = typeof(T).GetProperties();
            bool flag = false;
            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(obj);
                // 如果这个属性存在的话
                if (value != null)
                {
                    // 从注解厘米拿到具体的字段名称，拼接
                    var objAttrs = properties[i].GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        if (attr != null)
                        {
                            if (flag)
                            {
                                resWhereQuery.Append(" AND");
                            }
                            resWhereQuery.Append(" ");
                            resWhereQuery.Append(attr.Name);
                            resWhereQuery.Append("=@");
                            resWhereQuery.Append(attr.Name);
                            flag = true;
                        }
                    }
                }
            }

            var temp = resWhereQuery.ToString();
            if (temp.Contains("=@"))
            {
                return "WHERE " + temp;
            }
            else
            {
                return "";
            }
        }
    }
}