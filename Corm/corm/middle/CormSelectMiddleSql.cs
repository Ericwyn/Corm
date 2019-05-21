using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        private string sqlBuff;
        private string tableName;
        // 缓存该类型的列名，避免经常反射
        private List<string> columnNameTemp;
        
        
        //private string whereTemp = "";
        private T whereObj;
        // 查找的属性
        private string attributes = "*";
        // 自定义的 Sql 查询语句
        private SqlCommand customizeSqlCommand;
        // Top 数量
        private int topNum = -1;
        
        // like 查询
        private List<LikeQueryStruct> likeQueryList;
        
        public CormSelectMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
            this.columnNameTemp = cormTable.ColumnNameTemp;
        }

        // 查询全部
        public CormSelectMiddleSql<T> All()
        {
            this.attributes = "*";
            return this;
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
            this.whereObj = set;
            return this;
        }
        
        // Like 查询, 字符串 Like
        public CormSelectMiddleSql<T> WhereLike(string columnName, string likeStr)
        {
            if (likeQueryList == null)
            {
                likeQueryList = new List<LikeQueryStruct>();
            }
            likeQueryList.Add(new LikeQueryStruct()
            {
                column = columnName,
                query = likeStr,
            });
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
        
        public List<T> Commit(SqlTransaction transaction)
        {
            if (customizeSqlCommand != null && 
                (!attributes.Equals("*") || whereObj != null))
            {
                throw new CormException("SELECT 错误，Customize() 方法与其他查询方法冲突，请检查");
            }

            SqlDataReader reader;
            List<T> resList;
            if (customizeSqlCommand != null)
            {
                if (transaction != null)
                {
                    customizeSqlCommand.Transaction = transaction;
                }
                reader = customizeSqlCommand.ExecuteReader();
                resList = parseSqlDataReader(reader);
                return resList;
            }
            // 拼接 TOP 语句
            var topQuery = "";
            if (this.topNum >= 0)
            {
                topQuery = " TOP(" + topNum + ") ";
            }

            sqlBuff = "SELECT " + topQuery + attributes + " FROM " + this.tableName +" ";
            // 拼接 Where 语句
            sqlBuff = sqlBuff + "\n" + GetWhereQuery(this.whereObj) + " ";
            // 拼接 LIKE 字符串语句
            var whereLikeQUery = GetWhereLikeQuery(this.likeQueryList);
            if (whereLikeQUery != null && !whereLikeQUery.Trim().Equals(""))
            {
                if (sqlBuff.Contains("WHERE"))
                {
                    sqlBuff = sqlBuff + " AND " + whereLikeQUery;
                }
                else
                {
                    sqlBuff = sqlBuff + " WHERE " + whereLikeQUery;
                }

            }
            // 拼接 ";"
            sqlBuff += ";";
            var sqlCommend = new SqlCommand(sqlBuff, _cormTable._corm._sqlConnection);
//            if (whereLikeQUery != null && !whereLikeQUery.Trim().Equals("")) sqlCommend.Parameters.Add(GetWhereLikeParam(likeQueryList));
            if (whereLikeQUery != null && !whereLikeQUery.Trim().Equals(""))
            {
                foreach (var parameter in GetWhereLikeParam(likeQueryList))
                {
                    sqlCommend.Parameters.Add(parameter);
                }
            }
            var properties = typeof(T).GetProperties();

            if (sqlBuff.Contains("WHERE "))
            {
                // 有 Where 的话就要放入值
                foreach (var property in properties)
                {
                    var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        if (attr != null)
                        {
                            
                            if (sqlBuff.Contains("@"+attr.Name))
                            {
                                // 证明预编译语句里面有这个属性
                                var value = property.GetValue(this.whereObj);
                                var param = new SqlParameter("@"+attr.Name, attr.DbType, attr.Size);
                                param.Value = value;
                                sqlCommend.Parameters.Add(param);
                            }
                        }
                    }
                }
            }
            this._cormTable.SqlLog(sqlBuff);
            if (transaction != null)
            {
                sqlCommend.Transaction = transaction;
            }
            reader = sqlCommend.ExecuteReader();
            resList = parseSqlDataReader(reader);
            return resList;
        }

        // 使用自定义的 Sql 语句进行查询
        public CormSelectMiddleSql<T> Customize(string sqlStr)
        {
            return Customize(sqlStr, null);
        }

        public CormSelectMiddleSql<T> Customize(string sqlStr, SqlParameter[] parameters)
        {
            if (sqlStr == null || sqlStr.Trim().Equals(""))
            {
                throw new Exception("SELECT 使用 Customize() 方法进行自定义查询的时候，传入的 Sql 语句有误");
            }
            
            this.customizeSqlCommand = new SqlCommand(sqlStr, this._cormTable._corm._sqlConnection);
            if (parameters != null && parameters.Length != 0)
            {
                foreach (var parameter in parameters)
                {
                    customizeSqlCommand.Parameters.Add(parameter);
                }
            }

            return this;
        }
        
        
        /*
         * 解析 SqlDataReader
         */
        private List<T> parseSqlDataReader(SqlDataReader reader)
        {
            var resList = new List<T>();
            
            while(reader.Read()) {
                var objTemp = new T();
                foreach (var property in typeof(T).GetProperties())
                {
                    var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        try
                        {
                            if (reader[attr.Name] != null)
                            {
                                if (reader[attr.Name] is DBNull)
                                {
                                    property.SetValue(objTemp, null);
                                }
                                else
                                {
                                    property.SetValue(objTemp, reader[attr.Name]);
                                }
                            }
                        }
                        catch (IndexOutOfRangeException e)
                        {
                            // 只查询特定字段的时候会触发此处异常
                        }
                    }
                }
                resList.Add(objTemp);
            }
            reader.Close();
            return resList;
        }
        
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
                            resWhereQuery += " " + attr.Name + "=@" + attr.Name + " and";
                        }
                    }
                }
            }
            if (resWhereQuery.EndsWith("and"))
            {
                resWhereQuery = "WHERE "+resWhereQuery.Substring(0, resWhereQuery.Length - 4);
            }
            return resWhereQuery;
        }
        
        /*
         * 得到 Like 语句部分
         */
        private static string GetWhereLikeQuery(List<LikeQueryStruct> list)
        {
            if (list == null){
                return "";
            }
            var resWhereQuery = "";
            
            for (int i = 0; i < list.Count; i++)
            {
                resWhereQuery += list[i].column + " LIKE @WHERE_LIKE_PARAM_" + i + " AND ";
            }
            if (resWhereQuery.Length > 4)
            {
                resWhereQuery = resWhereQuery.Substring(0, resWhereQuery.Length - 4);
            }
            return resWhereQuery;
        }
        
        /*
         * 得到 Like 预编译语句的 SQLParamter
         */
        private static List<SqlParameter> GetWhereLikeParam(List<LikeQueryStruct> list)
        {
            if (list == null){
                return null;
            }
            var resWhereQuery = new List<SqlParameter>();
            for (int i = 0; i < list.Count; i++)
            {
                resWhereQuery.Add(new SqlParameter("@WHERE_LIKE_PARAM_"+i, "%" + list[i].query + "%"));
            }
            return resWhereQuery;
        }
        
        public class LikeQueryStruct
        {
            public string column { get; set; }
            public string query { get; set; }
        }
    }
}