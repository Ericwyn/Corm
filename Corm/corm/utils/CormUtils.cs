using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CORM.attrs;

namespace CORM.utils
{
    public class CormUtils<T>
    {
        public static string GetTableName()
        {
            var properties = typeof(T).GetProperties();
            // 自动分析 TableName
            var tableAttributes = typeof(T).GetCustomAttributes(typeof(Table), true);
            if (tableAttributes.Length > 0)
            {
                Table tableAttr = tableAttributes[0] as Table;
                if (tableAttr != null && tableAttr.TableName != null && !tableAttr.TableName.Trim().Equals(""))
                {
                    return tableAttr.TableName;
                }
            }

            return null;
        }

        public static Dictionary<string, PropertyInfo> GetProPropertyInfoMap()
        {
            var PropertyMap = new Dictionary<string, PropertyInfo>();
            foreach (var property in typeof(T).GetProperties())
            {
                var objAttrs = property.GetCustomAttributes(typeof(Column), true);
                if (objAttrs.Length > 0)
                {
                    Column attr = objAttrs[0] as Column;
                    if (attr != null)
                    {
                        PropertyMap.Add(attr.Name, property);
                        continue;
                    }
                }
                PropertyMap.Add(property.Name, property);
            }

            return PropertyMap;
        }

        // 将一个 string 的数据库类型描述，转换成 DbType
        public static SqlDbType parseDbType(string typeStr)
        {
            var type = typeStr.ToLower();
            switch (type)
            {
                case "bigint":
                    return SqlDbType.BigInt;
                case "binary":
                    return SqlDbType.Binary;
                case "bit":
                    return SqlDbType.Bit;
                case "char":
                    return SqlDbType.Char;
                case "datetime":
                    return SqlDbType.DateTime;
                case "decimal":
                    return SqlDbType.Decimal;
                case "float":
                    return SqlDbType.Float;
                case "image":
                    return SqlDbType.Image;
                case "int":
                    return SqlDbType.Int;
                case "money":
                    return SqlDbType.Money;
                case "nchar":
                    return SqlDbType.NChar;
                case "ntext":
                    return SqlDbType.NText;
                case "nvarchar":
                    return SqlDbType.NVarChar;
                case "real":
                    return SqlDbType.Real;
                case "uniqueidentifier":
                    return SqlDbType.UniqueIdentifier;
                case "smalldatetime":
                    return SqlDbType.SmallDateTime;
                case "smallint":
                    return SqlDbType.SmallInt;
                case "smallmoney":
                    return SqlDbType.SmallMoney;
                case "text":
                    return SqlDbType.Text;
                case "timestamp":
                    return SqlDbType.Timestamp;
                case "tinyint":
                    return SqlDbType.TinyInt;
                case "varbinary":
                    return SqlDbType.VarBinary;
                case "varchar":
                    return SqlDbType.VarChar;
                case "variant":
                    return SqlDbType.Variant;
                case "xml":
                    return SqlDbType.Xml;
                case "udt":
                    return SqlDbType.Udt;
                case "structured":
                    return SqlDbType.Structured;
                case "date":
                    return SqlDbType.Date;
                case "time":
                    return SqlDbType.Time;
                case "datetime2":
                    return SqlDbType.DateTime2;
                case "datetimeoffset":
                    return SqlDbType.DateTimeOffset;
                default:
                    throw new CormException("解析 String 格式的 DbType 时候发生错误，无法解析的 String 为 : " + typeStr);
            }
        }
    }
}