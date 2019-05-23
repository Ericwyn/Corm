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
            return parse(reader, closeReader, false);
        }
        
        public static List<T> parse(SqlDataReader reader, bool closeReader, bool noCloumn)
        {
            var resList = new List<T>();
            
            while(reader.Read()) {
                var objTemp = new T();
                foreach (var property in typeof(T).GetProperties())
                {
                    try
                    {
                        if (noCloumn)
                        {
                            if (reader[property.Name] != null)
                            {
                                if (reader[property.Name] is DBNull)
                                {
                                    property.SetValue(objTemp, null);
                                }
                                else
                                {
                                    property.SetValue(objTemp, reader[property.Name]);
                                }

                            }
                        }
                        else
                        {
                            var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                            if (objAttrs.Length > 0)
                            {
                                Column attr = objAttrs[0] as Column;
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
                        }
                    }
                    catch (IndexOutOfRangeException e)
                    {

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