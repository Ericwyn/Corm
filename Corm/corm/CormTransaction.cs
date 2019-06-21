using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CORM.utils;

namespace CORM
{
    public class CormTransSql
    {
        public SqlConnection _sqlConnection { get; set; }
        public SqlTransaction _transaction { get; set; }
        public string sqlBuff { get; set; }
        public List<SqlParameter> paramList { get; set; }

        public int ExecuteNonQuery()
        {
            SqlCommand sqlCommand = new SqlCommand(sqlBuff, _sqlConnection);
            sqlCommand.Transaction = _transaction;
            if (paramList != null)
            {
                foreach (SqlParameter param in paramList)
                {
                    sqlCommand.Parameters.Add(param);
                }
            }
            return sqlCommand.ExecuteNonQuery();
        }

        public SqlDataReader ExecuteReader()
        {
            SqlCommand sqlCommand = new SqlCommand(sqlBuff, _sqlConnection);
            sqlCommand.Transaction = _transaction;
            foreach (SqlParameter param in paramList)
            {
                sqlCommand.Parameters.Add(param);
            }
            return sqlCommand.ExecuteReader();
        }

//        public void CommitForReader()
//        {
//            
//        }
    }
    public class CormTransaction : IDisposable
    {
        public List<CormTransSql> transSql { get; set; }

        private SqlConnection _sqlConnection;
        private SqlTransaction _transaction;
        
        public CormTransaction(Corm corm)
        {
            this.transSql = new List<CormTransSql>();
            this._sqlConnection = corm.NewConnection();
            this._transaction = this._sqlConnection.BeginTransaction();
        }

        public CormTransSql AddSql(string sqlBuff, List<SqlParameter> paramList)
        {
            CormTransSql transSql = new CormTransSql()
            {
                _sqlConnection = this._sqlConnection,
                _transaction = this._transaction,
                sqlBuff = sqlBuff,
                paramList = paramList,
            };
            return transSql;
        }

        public void Commit()
        {
            this._transaction.Commit();
            if(_sqlConnection.State == ConnectionState.Open)  
            {  
                _sqlConnection.Close();  
            }          
        }

        public void Rollback()
        {
            this._transaction.Rollback();
            if(_sqlConnection.State == ConnectionState.Open)  
            {  
                _sqlConnection.Close();  
            } 
        }
        
        public void Dispose()
        {
            if(_sqlConnection.State == ConnectionState.Open)  
            {  
                _sqlConnection.Close();  
            } 
            _sqlConnection?.Dispose();
            _transaction?.Dispose();
        }
    }
}