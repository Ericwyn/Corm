using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CORM.utils;

namespace CORM
{
    /**
     * 自定义 SQL 语句的调用工具类，抽取自原先的 CormSelectMiddle
     */
    public class CormCustomizeMiddleSql<T> where T : new()
    {
        private CormTable<T> _cormTable;
        private string sqlBuff;
        private string tableName;
        // 缓存该类型的列名，避免经常反射
        private List<string> columnNameTemp;
        
        //--------------
        private string customizeSqlBuff = "";
        private List<SqlParameter> customizeSqlParamList;
        
        public CormCustomizeMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
//            this.columnNameTemp = cormTable.ColumnNameTemp;
        }
        
        public CormCustomizeMiddleSql<T> SQL(string sqlStr)
        {
            return SQL(sqlStr, new List<SqlParameter>());
        }

        public CormCustomizeMiddleSql<T> SQL(string sqlStr, SqlParameter[] parameters)
        {
            return SQL(sqlStr, new List<SqlParameter>(parameters));
        }
        
        public CormCustomizeMiddleSql<T> SQL(string sqlStr, List<SqlParameter> parameters)
        {
            if (sqlStr == null || sqlStr.Trim().Equals(""))
            {
                throw new CormException("使用 Customize() 进行自定义查询的时候，传入的 Sql 语句有误");
            }
            this.customizeSqlBuff = sqlStr;
            if (parameters != null)
            {
                if (this.customizeSqlParamList == null)
                {
                    this.customizeSqlParamList = new List<SqlParameter>();
                }
                foreach (SqlParameter parameter in parameters)
                {
                    this.customizeSqlParamList.Add(parameter);
                }
            }
            return this;
        }
        
        /*
         * 无返回的 SQL 运行
         */
        public int CommitForNone()
        {
            return CommitForNone(null);
        }

        /*
         * 有返回的 SQL 运行
         */
        public int CommitForNone(CormTransaction transaction)
        {
            int resLineCount = -1;
            SqlCommand sqlCommand;
            if (transaction != null)
            {
                resLineCount = transaction.AddSql(customizeSqlBuff, customizeSqlParamList).ExecuteNonQuery();
            }
            else
            {
                using (SqlConnection conn = this._cormTable._corm.NewConnection())
                {
                    sqlCommand = new SqlCommand(customizeSqlBuff, conn);
                    if (customizeSqlParamList != null && customizeSqlParamList.Count > 0)
                    {
                        foreach (SqlParameter param in customizeSqlParamList)
                        {
                            sqlCommand.Parameters.Add(param);
                        }
                    }
                    resLineCount = sqlCommand.ExecuteNonQuery();
                }
            }
            return resLineCount;
        }
        
        public List<T> CommitForList()
        {
            return CommitForList(null);
        }
        
        public List<T> CommitForList(CormTransaction transaction)
        {
            var reader = CommitForReader(transaction);
            var resList = SqlDataReaderParse<T>.parse(reader, true);
            return resList;
        }
        
        public SqlDataReader CommitForReader()
        {
            return CommitForReader(null);
        }
        
        public SqlDataReader CommitForReader(CormTransaction transaction)
        {
            SqlDataReader reader = null;
            SqlCommand sqlCommand;
            if (!customizeSqlBuff.Equals(""))
            {
                if (transaction != null)
                {
                    reader = transaction.AddSql(customizeSqlBuff, customizeSqlParamList).ExecuteReader();
                }
                else
                {
                    sqlCommand = new SqlCommand(customizeSqlBuff, this._cormTable._corm.NewConnection());
                    if (customizeSqlParamList != null && customizeSqlParamList.Count > 0)
                    {
                        foreach (SqlParameter param in customizeSqlParamList)
                        {
                            sqlCommand.Parameters.Add(param);
                        }
                    }
                    reader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
                }
                return reader;
            }
            else
            {
                throw new CormException("使用 Customize() 进行自定义查询的时候，传入的 Sql 语句有误");
            }
        }
        
        
    }
}