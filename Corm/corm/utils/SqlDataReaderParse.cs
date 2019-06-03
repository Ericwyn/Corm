using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Reflection.Emit;
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
                
                foreach (PropertyInfo property in typeof(T).GetProperties())
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

        public static List<T> NewParse(SqlDataReader reader, bool closeReader, bool noCloumn)
        {
            var resList = new List<T>();
            var builder = IDataReaderEntityBuilder<T>.CreateBuilder(reader, noCloumn);
            while (reader.Read())
            {
                var entity = builder.Build(reader);
                resList.Add(entity);
            }
            if (closeReader)
            {
                reader.Close();
            }
            return resList;
        }
        
    }
    
    
    public class IDataReaderEntityBuilder<Entity>
    {
        private static readonly MethodInfo getValueMethod = typeof(IDataRecord).GetMethod("get_Item", new Type[] { typeof(int) });
        private static readonly MethodInfo isDBNullMethod = typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });
        private delegate Entity Load(IDataRecord dataRecord);
 
        private Load handler;
        private IDataReaderEntityBuilder() { }
 
        public Entity Build(IDataRecord dataRecord) { return handler(dataRecord); }

        // 新的解析工具类
        // 参考 “利用反射将IDataReader读取到实体类中效率低下的解决办法”
        //     https://blog.csdn.net/lilong_herry/article/details/79993907 
        public static IDataReaderEntityBuilder<Entity> CreateBuilder(IDataRecord dataRecord, bool noCloumn)
        {
            IDataReaderEntityBuilder<Entity> dynamicBuilder = new IDataReaderEntityBuilder<Entity>();
            DynamicMethod method = new DynamicMethod("IDataReaderDynamicCreateEntity", typeof(Entity), new Type[] { typeof(IDataRecord) }, typeof(Entity), true);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder result = generator.DeclareLocal(typeof(Entity));
            generator.Emit(OpCodes.Newobj, typeof(Entity).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);
 
            // 反射一次拿到所有属性存入一个 Map 里面
            Dictionary<string, PropertyInfo> propertyMap = new Dictionary<string , PropertyInfo>();
            foreach (PropertyInfo property in typeof(Entity).GetProperties())
            {
                // 没有 Column 注解的话，ColumnName 要等于 Property 的 Name 才可以
                if (noCloumn)
                {
                    propertyMap.Add(property.Name, property);
                    
                }
                // 有 Column 注解的话，columnName 要等于 column 注解的 Name
                else
                {
                    var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                    if (objAttrs.Length > 0)
                    {
                        propertyMap.Add((objAttrs[0] as Column).Name, property);
                    }
                }
            }
            
            for (int i = 0; i < dataRecord.FieldCount; i++)
            {
                PropertyInfo propertyInfo = null;
                try
                {
                    Console.WriteLine(dataRecord.GetName(i));
                    propertyInfo = propertyMap[dataRecord.GetName(i)];
                }
                catch (System.Collections.Generic.KeyNotFoundException e)
                {
                    propertyInfo = null;
                }
                // 从 propertyInfo 里面找到与名字一样的 Reader 的 key
//                PropertyInfo propertyInfo = typeof(Entity).GetProperty(properties.FirstOrDefault(x=>(((Column)x.GetCustomAttributes(typeof(Column), true)[0]).Name!=null?).Equals(dataRecord.GetName(i)))?.Name);
                Label endIfLabel = generator.DefineLabel();
                if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, isDBNullMethod);
                    generator.Emit(OpCodes.Brtrue, endIfLabel);
                    generator.Emit(OpCodes.Ldloc, result);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, getValueMethod);
                    generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                    generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                    generator.MarkLabel(endIfLabel);
                }
            }
            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);
            dynamicBuilder.handler = (Load)method.CreateDelegate(typeof(Load));
            return dynamicBuilder;
        }
    }
}