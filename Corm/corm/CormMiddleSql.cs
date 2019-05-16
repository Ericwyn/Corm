using System;
using System.Collections.Generic;
using System.Data;
using Corm.attrs;

namespace Corm
{
    // 用来保存查询语句的中间状态
    public class CormSelectMiddleSql<T>
    {
        private CormTable<T> _cormTable;
        private string sqlBuff;
        // 查找的属性
        private string attributes = "*";
        private string tableName;
//        private string whereTemp = "";
        private T whereEntity;
        
        public CormSelectMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
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
            this.whereEntity = set;
            return this;
        }

        // TODO LIMIT ,SqlServer 的 LIMIT 语句也太反人类了吧！！！
//        // Limit 
//        public CormSelectMiddleSql<T> Limit(int start, int end)
//        {
//            
//        }
        
        
        public List<T> Commit()
        {
            sqlBuff = "SELECT " + attributes + " FROM " + this.tableName +" ";
            // 拼接 where
//            if (!whereTemp.Equals(""))
//            {
//                sqlBuff = sqlBuff + "\n" + whereTemp +" ";
//            }
            sqlBuff = sqlBuff + "\n" + getWhereQuery() + " ";

            // TODO 拼接 limit
            
            // 拼接 ";"
            sqlBuff += ";";
            // 放入值
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var objAttrs = property.GetCustomAttributes(typeof(CormColumn), true);
                if (objAttrs.Length > 0)
                {
                    CormColumn attr = objAttrs[0] as CormColumn;
                    if (attr != null)
                    {
                        
//                        Console.WriteLine(attr.Name);
//                        Console.WriteLine(attr.Length);
//                        Console.WriteLine(attr.DbType.ToString());
                    }
                }
            }
            
            
//            this._cormTable._corm._sqlConnection.     
            Console.WriteLine(sqlBuff);
            return null;
        }

        private string getWhereQuery()
        {
            if (this.whereEntity == null){
               return "";
            }
            var resWhereQuery = "";
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(this.whereEntity);
                // 如果这个属性存在的话
                if (value != null)
                {
                    // 从注解厘米拿到具体的字段名称，拼接
                    var objAttrs = property.GetCustomAttributes(typeof(CormColumn), true);
                    if (objAttrs.Length > 0)
                    {
                        CormColumn attr = objAttrs[0] as CormColumn;
                        if (attr != null)
                        {
                            resWhereQuery += " " + attr.Name + "=@" + attr.Name + " and";
                        }
                    }
                }
                
            }

            // 去除最后的一个 “and ” 
            if (resWhereQuery.EndsWith("and"))
            {
                resWhereQuery = "WHERE "+resWhereQuery.Substring(0, resWhereQuery.Length - 4);
            }
            return resWhereQuery;
        }
        
    }
}