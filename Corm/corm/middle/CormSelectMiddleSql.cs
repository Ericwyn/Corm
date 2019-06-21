using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using CORM.attrs;
using CORM.utils;

namespace CORM
{
    /*
     * 用来保存查询语句的中间状态
     */
    public class CormSelectMiddleSql<T> where T : new()
    {
        private CormTable<T> _cormTable;
        private StringBuilder sqlBuilder = new StringBuilder("");
        private string tableName;
        // 缓存该类型的列名，避免经常反射
        private Dictionary<string, PropertyInfo> PropertyMap;
        
        // 用以设置 where 条件的 obj
        private T whereObj;
        // 查找的属性
        private string attributes = "*";

        // Top 数量
        private int topNum = -1;

        private string[] orderByAttributes = {};
        private string[] orderDescByAttributes = {};
        
        // 自定义 WhereQuery 查询语句
        private string cusWhereQuery = null;
        // 自定义 WhereQuery 查询的参数
        private SqlParameter[] cusWhereQueryParams; 
        
        public CormSelectMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
            this.PropertyMap = cormTable.PropertyMap;
        }

        // 查询几个属性
        public CormSelectMiddleSql<T> Attributes(string[] columnNames)
        {
            string temp = "";
            foreach (var column in columnNames)
            {
                temp += "" + this.tableName + "." + column + ",";
            }
            this.attributes = temp.Substring(0, temp.Length - 1);
            return this;
        }
        
        // Where 查询
        public CormSelectMiddleSql<T> Where(T set)
        {
            if (cusWhereQuery != null && !cusWhereQuery.Equals(""))
            {
                throw new CormException("SELECT 操作中, Where() 方法和 WhereQuery() 方法不可同时使用");
            }
            this.whereObj = set;
            return this;
        }
        
        
        public CormSelectMiddleSql<T> WhereQuery(string query)
        {
            return WhereQuery(query, null);
        }
        
        // 自定义 Where 查询
        // 该方法不能和 Where 方法同时使用
        // query 是自定义的 where 语句，例如  "and a > b" , "or a < 10" 之类的
        // parameters 是自定义 where 语句的参数
        public CormSelectMiddleSql<T> WhereQuery(string query, SqlParameter[] parameters)
        {
            if (whereObj != null)
            {
                throw new CormException("SELECT 操作中, Where() 方法和 WhereQuery() 方法不可同时使用");
            }
            if (parameters.Length > 0)
            {
                cusWhereQueryParams = parameters;
            }

            if (query != null && !query.Equals(""))
            {
                cusWhereQuery = query;
            }
            return this;
        }

        // 正序排列
        public CormSelectMiddleSql<T> OrderBy(string[] atts)
        {
            this.orderByAttributes = atts;
            return this;
        }

        // 倒序排列
        public CormSelectMiddleSql<T> OrderDescBy(string[] atts)
        {
            this.orderDescByAttributes = atts;
            return this;
        }
        // Top 设置
        public CormSelectMiddleSql<T> Top(int num)
        {
            this.topNum = num;
            return this;
        }

        public List<T> Commit()
        {
            return Commit(null);
        }
        
        public List<T> Commit(CormTransaction transaction)
        {
            var reader = CommitForReader(transaction);
            var resList = SqlDataReaderParse<T>.parse(reader, true);
            return resList;
        }

        public T CommitForOne()
        {
            return CommitForOne(null);
        }
        
        public T CommitForOne(CormTransaction transaction)
        {
            // 只查找一个
            this.Top(1);
            List<T> temp = Commit(transaction);
            if (temp.Count != 1)
            {
                return default(T);
            }
            else
            {
                return temp[0];
            }
        }
        
        public SqlDataReader CommitForReader()
        {
            return CommitForReader(null);
        }
        
        public SqlDataReader CommitForReader(CormTransaction transaction)
        {

            SqlDataReader reader;
            SqlCommand sqlCommand;

            // 拼接 TOP 语句
            var topQuery = "";
            if (this.topNum >= 0)
            {
                topQuery = " TOP(" + topNum + ") ";
            }
            
            // "SELECT " + topQuery + attributes + " FROM " + this.tableName +" "
            sqlBuilder.Append("SELECT ");
            sqlBuilder.Append(topQuery);
            sqlBuilder.Append(attributes);
            sqlBuilder.Append(" FROM ");
            sqlBuilder.Append(this.tableName);
            sqlBuilder.Append(" ");

            // 拼接 Where 语句
            // "\n" + GetWhereQuery(this.whereObj) + " "
            sqlBuilder.Append("\n");
            // 拼接 WhereObj 得到的 Where 语句
            if (whereObj != null)
            {
                sqlBuilder.Append(GetWhereQuery(this.whereObj)); 
            }
            // 拼接用户自定义的 WhereQuery
            else if (cusWhereQuery != null && !cusWhereQuery.Equals(""))
            {
                sqlBuilder.Append(" WHERE ").Append(cusWhereQuery);
            }
            sqlBuilder.Append(" ");
 
            sqlBuilder.Append(GetOrderQuery(this.orderByAttributes, this.orderDescByAttributes));
            // 拼接 ";"
            sqlBuilder.Append(" ;");
            List<SqlParameter> paramList = new List<SqlParameter>();

            var sql = sqlBuilder.ToString();
            if (this.whereObj != null && sql.Contains("WHERE "))
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
            }

            if (cusWhereQueryParams != null)
            {
                foreach (SqlParameter param in cusWhereQueryParams)
                {
                    paramList.Add(param);
                }
            }
            
            this._cormTable.SqlLog(sql);
            if (transaction != null)
            {
                reader = transaction.AddSql(sql, paramList).ExecuteReader();
            }
            else
            {
                    sqlCommand = new SqlCommand(sql, _cormTable._corm.NewConnection());
                    foreach (SqlParameter param in paramList)
                    {
                        sqlCommand.Parameters.Add(param);
                    }
                    reader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
            }
            return reader;
        }
        
        // 判断 Select 是否能够查找到对象
        public bool CommitForHas()
        {
            return CommitForHas(null);
        }
        public bool CommitForHas(CormTransaction transaction)
        {
            using (var reader = CommitForReader(transaction))
            {
                var has = reader.HasRows;
                reader.Close();
                return has;
            }
        }
        
        // 得到 Sort 语句
        private static string GetOrderQuery(string[] orderByList, string[] orderDescByList)
        {
            var resOrderQuery = new StringBuilder("");
            if ((orderByList == null || orderByList.Length == 0) && (orderDescByList == null || orderDescByList.Length == 0))
            {
                return "";
            }

            resOrderQuery.Append(" ORDER BY ");
            for (int i = 0; i < orderByList.Length ; i++)
            {
                resOrderQuery.Append(orderByList[i]);
                resOrderQuery.Append(" ASC");
                if ( i != orderByList.Length - 1)
                {
                    resOrderQuery.Append(", ");
                }
            }

            for (int i = 0; i < orderDescByList.Length; i++)
            {
                if (i == 0)
                {
                    if (resOrderQuery.ToString().Contains("ASC"))
                    {
                        resOrderQuery.Append(", ");
                    }
                }
                resOrderQuery.Append(orderDescByList[i]);
                resOrderQuery.Append(" DESC");
                if (i != orderDescByList.Length - 1)
                {
                    resOrderQuery.Append(", ");
                }
            }
            return resOrderQuery.ToString();
        }
        
        // 得到 where 语句
        private static string GetWhereQuery(T obj)
        {
            if (obj == null){
                return "";
            }
            var resWhereQuery = new StringBuilder("");
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                // 如果这个属性存在的话
                if (value != null)
                {
                    // 从注解里面拿到具体的字段名称，拼接
                    var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        if (attr != null)
                        {
                            // " " + attr.Name + "=@" + attr.Name + " and"
                            resWhereQuery.Append(" ");
                            resWhereQuery.Append(attr.Name);
                            resWhereQuery.Append("=@");
                            resWhereQuery.Append(attr.Name);
                            resWhereQuery.Append(" AND");
                        }
                    }
                }
            }

            var temp = resWhereQuery.ToString();
            if (temp.EndsWith("AND"))
            {
                return "WHERE "+temp.Substring(0, temp.Length - 3);
            }
            return "";
        }
        
    }
}