using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Corm.attrs;
using Corm.utils;

namespace Corm
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
        private T whereEntity;
        // 查找的属性
        private string attributes = "*";
        
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
            // 拼接 Where 语句
            sqlBuff = sqlBuff + "\n" + GetWhereQuery(this.whereEntity) + " ";
            // TODO 拼接 TOP
            // 拼接 ";"
            sqlBuff += ";";
            var sqlCommend = new SqlCommand(sqlBuff, _cormTable._corm._sqlConnection);
            var properties = typeof(T).GetProperties();

            if (sqlBuff.Contains("WHERE "))
            {
                // 有 Where 的话就要放入值
                foreach (var property in properties)
                {
                    var objAttrs = property.GetCustomAttributes(typeof(CormColumn), true);
                    if (objAttrs.Length > 0)
                    {
                        CormColumn attr = objAttrs[0] as CormColumn;
                        if (attr != null)
                        {
                            
                            if (sqlBuff.Contains("@"+attr.Name))
                            {
                                // 证明预编译语句里面有这个属性
                                var value = property.GetValue(this.whereEntity);
                                var param = new SqlParameter("@"+attr.Name, attr.DbType, attr.Size);
                                param.Value = value;
                                sqlCommend.Parameters.Add(param);
                            }
                        }
                    }
                }
            }
            CormLog.ConsoleLog(sqlCommend.CommandText);
            var reader = sqlCommend.ExecuteReader();
            var resList = new List<T>();
            
            while(reader.Read()) {
                var objTemp = new T();
                foreach (var property in properties)
                {
                    var objAttrs = property.GetCustomAttributes(typeof(CormColumn), true);
                    if (objAttrs.Length > 0)
                    {
                        CormColumn attr = objAttrs[0] as CormColumn;
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
            if (resWhereQuery.EndsWith("and"))
            {
                resWhereQuery = "WHERE "+resWhereQuery.Substring(0, resWhereQuery.Length - 4);
            }
            return resWhereQuery;
        }
        
    }
}