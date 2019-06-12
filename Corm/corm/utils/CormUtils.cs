using System.Collections.Generic;
using System.IO;
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
    }
}