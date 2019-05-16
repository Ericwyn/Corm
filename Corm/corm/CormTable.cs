using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Xml.Serialization;

namespace Corm
{
    public class CormTable<T>
    {
        public Corm _corm { get; }
        public string _tableName { get; }
//        private T _entity;
        
        public CormTable(Corm corm ,string tableName)
        {
            this._corm = corm;
            this._tableName = tableName;
//            this._entity = entity;
        }

        
        
        
        public CormSelectMiddleSql<T> Find()
        {
            return new CormSelectMiddleSql<T>(this);
        }


        public SqlCommand SqlCommand(string sql)
        {
            return new SqlCommand(sql, this._corm._sqlConnection);
        }
        
    }
    
    
}