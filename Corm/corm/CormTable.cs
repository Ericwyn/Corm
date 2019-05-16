using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Corm
{
    public class CormTable<T>
    {
        public Corm _corm { get; }
        public string _tableName { get; }
        private T _entity;
        
        public CormTable(Corm corm, T entity ,string tableName)
        {
            this._corm = corm;
            this._tableName = tableName;
            this._entity = entity;
        }

        
        
        
        public CormSelectMiddleSql<T> findAll()
        {
            return new CormSelectMiddleSql<T>(this);
        }
        
        
    }
    
    
}