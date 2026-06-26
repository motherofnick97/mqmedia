using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.CommonFunc
{
    public class CommonFunction
    {
        public static T? ParseEnumByDescription<T>(string description) where T : struct, Enum
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<DescriptionAttribute>();
                if (attr != null && attr.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    return (T)field.GetValue(null);
            }
            return null;
        }
    }
}
