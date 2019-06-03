using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using CORM.attrs;
using CORM.utils;

namespace CORM
{
    /**
     * 用来存储 Insert 语句的中间状态
     */
    public class CormInsertMiddleSql<T> where T : new ()
    {
        private CormTable<T> _cormTable;
        private string sqlBuff;
        private string tableName;
        // 缓存 Property
        private Dictionary<string, PropertyInfo> PropertyMap;
        // 缓存该类型的列名，避免经常反射, 使用数组保证顺序一致
        private string[] columnNameArrary;

        private T insertTemp;
        private List<T> insertTempList;
        
        public CormInsertMiddleSql(CormTable<T> cormTable)
        {
            this._cormTable = cormTable;
            this.tableName = _cormTable._tableName;
            this.PropertyMap = _cormTable.PropertyMap;
            this.columnNameArrary = new string[this.PropertyMap.Keys.Count];
            var index = 0;
            foreach (string key in this.PropertyMap.Keys)
            {
                columnNameArrary[index++] = key;
            }
        }
        
        // 插入一条数据
        public CormInsertMiddleSql<T> Value(T entity)
        {
            this.insertTemp = entity;
            return this;
        }

        // 插入多条数据
        public CormInsertMiddleSql<T> Value(List<T> entityList)
        {
            this.insertTempList = entityList;
            return this;
        }

        public int Commit()
        {
            return Commit(null);
        }
        
        /*
         * 带有事务属性的提交操作
         * 由于这里使用拼接一条字符串操作，多行插入数据也会放在同一条 sql 当中
         * 而这条 sql 是动态拼接的
         * 所以如果一次性插入的数据过多的话，有可能这条 sql 的大小会变得很夸张，造成内存泄漏等问题
         * TODO 分成多次执行，例如当一次性插入数量超过 1000 的时候，分成多个批次，每个批次 1000 行
         * 
         */
        public int Commit(CormTransaction transaction)
        {
            // 通过这个 Flag 来标记插入多行数据时候，每一行不同的占位符
            // 占位符规则为 "@" + columnName + flag + itemIndex
            var flagForListItem = "COUNT_";
            
            if (insertTemp == null && insertTempList == null)
            {
                throw new Exception(" [Corm] 调用 Insert 方法时候添加插入数据");
            }
            if (insertTempList == null)
            {
                insertTempList = new List<T>();
            }

            if (insertTemp != null)
            {
                insertTempList.Add(insertTemp);
            }
            
            sqlBuff = "INSERT INTO " + this.tableName + "(";
            foreach (var columnName in columnNameArrary)
            {
                sqlBuff += columnName + ",";
            }
            sqlBuff = sqlBuff.Substring(0, sqlBuff.Length - 1);
            sqlBuff += ") VALUES ";
            // 开始拼接字符串
            for (var i = 0; i < insertTempList.Count; i++)
            {
                sqlBuff += "\n(";
                foreach (var colunmName in columnNameArrary)
                {
                    sqlBuff += "@" + colunmName + flagForListItem + i +",";
                }

                sqlBuff = sqlBuff.Substring(0, sqlBuff.Length - 1);
                sqlBuff += "),";
            }

            sqlBuff = sqlBuff.Substring(0, sqlBuff.Length - 1);
            sqlBuff += ";";

            
            // 开始执行事务
//            var sqlCommand = new SqlCommand(sqlBuff, this._cormTable._corm._sqlConnection);
            List<SqlParameter> paramList = new List<SqlParameter>(); 
            T insertObj;
            for (var i = 0; i < insertTempList.Count; i++)
            {
                insertObj = insertTempList[i];
                // 这里的值的排列循序需要按照 colunmNameTemp 的顺序
                foreach (var columnName in columnNameArrary)
                {
                    var param = new SqlParameter();
                    // 从注解拿到具体的字段名称，拼接
                    var objAttrs = PropertyMap[columnName].GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        Column attr = objAttrs[0] as Column;
                        if (attr != null)
                        {
                            // 创建 param 以填充 sqlBuff 当中的占位符
                            param = new SqlParameter("@" + attr.Name + flagForListItem + i, attr.DbType, attr.Size);
                            var value = PropertyMap[columnName].GetValue(insertObj);
                            // 如果这个属性存在的话
                            if (value != null)
                            {
                                param.Value = value;
                            }
                            else
                            {
                                // 当找不到值的时候就用 DBNull.value 代替，插入的时候将会插入 null
                                param.Value = DBNull.Value;
                            }
                            paramList.Add(param);
                        }
                    }
                }
            }

            this._cormTable.SqlLog(sqlBuff);
            int resColSize = -1;
            if (transaction != null)
            {
                // 如果是有事务操作的话，就把需要执行的语句保存到 CormTransaction 里面
                // 使用 Trans里面共同的 Connection
                // 和其他事务一起调用和返回
                resColSize = transaction.AddSql(sqlBuff, paramList).ExecuteNonQuery();
                if (resColSize < 0)
                {
                    throw new CormException(" INSERT 操作，受影响操作函数 < 0，请检查是否有错误");
                }
                return resColSize;
            }
            else
            {
                using (SqlConnection conn = this._cormTable._corm.NewConnection())
                {
                    var sqlCommand = new SqlCommand(sqlBuff, conn);
                    foreach (SqlParameter param in paramList)
                    {
                        sqlCommand.Parameters.Add(param);
                    }
                    if ((resColSize = sqlCommand.ExecuteNonQuery()) < 0)
                    {
                        throw new CormException(" INSERT 操作，受影响操作函数 < 0，请检查是否有错误");
                    }
                }
                return resColSize;
            }
        }
    }
}