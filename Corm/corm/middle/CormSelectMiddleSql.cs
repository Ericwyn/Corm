using System;
using System.Collections.Generic;
using System.Data;
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
        
                private T whereObj;
        // 查找的属性
        private string attributes = "*";

        // Top 数量
        private int topNum = -1;

        private string[] orderByAttributes = {};
        private string[] orderDescByAttributes = {};
        
        // like 查询
        private List<LikeQueryStruct> likeQueryList;
        
        public CormSelectMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
            this.columnNameTemp = cormTable.ColumnNameTemp;
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
            sqlBuff += GetOrderQuery(this.orderByAttributes, this.orderDescByAttributes);
            // 拼接 ";"
            sqlBuff += ";";
            List<SqlParameter> paramList = new List<SqlParameter>();
            if (whereLikeQUery != null && !whereLikeQUery.Trim().Equals(""))
            {
                foreach (var parameter in GetWhereLikeParam(likeQueryList))
                {
                    paramList.Add(parameter);
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
                                paramList.Add(param);
                            }
                        }
                    }
                }
            }
            this._cormTable.SqlLog(sqlBuff);
            if (transaction != null)
            {
                reader = transaction.AddSql(sqlBuff, paramList).ExecuteReader();
            }
            else
            {
                sqlCommand = new SqlCommand(sqlBuff, this._cormTable._corm.NewConnection());
                foreach (SqlParameter param in paramList)
                {
                    sqlCommand.Parameters.Add(param);
                }
                reader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
            }
            return reader;
        }
        
        
        // 得到 Sort 语句
        private static string GetOrderQuery(string[] orderByList, string[] orderDescByList)
        {
            var resOrderQuery = "";
            if ((orderByList == null || orderByList.Length == 0) && (orderDescByList == null || orderDescByList.Length == 0))
            {
                return resOrderQuery;
            }

            resOrderQuery = " ORDER BY ";
            foreach (var att in orderByList)
            {
                resOrderQuery += att + " ASC, ";
            }
            foreach (var att in orderDescByList)
            {
                resOrderQuery += att + " DESC, ";
            }
            resOrderQuery = resOrderQuery.Substring(0, resOrderQuery.Length - 2);
            return resOrderQuery;
        }
        
        // 得到 where 语句
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
                resWhereQuery = "WHERE "+resWhereQuery.Substring(0, resWhereQuery.Length - 3);
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