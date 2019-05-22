using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CORM.attrs;

namespace CORM.utils
{
    public class SqlDataReaderParse<T> where T : new()
    {
        public static List<T> parse(SqlDataReader reader, bool closeReader)
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

            if (closeReader)
            {
                reader.Close();
            }
            return resList;
        }
    }
}