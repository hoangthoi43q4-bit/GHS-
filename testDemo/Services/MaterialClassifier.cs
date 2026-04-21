using GHPHandShake.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHPHandShake.Services
{
    public static class MaterialClassifier
    {
        /// <summary>
        /// 根据输入字符串，匹配子类名并返回所属的物料类型
        /// </summary>
        
        public static string GetTypeBySubTypeMatch(string input , MaterialConfig config  )
        {
            int maxVal = 0;
            bool found = false;

            if (string.IsNullOrWhiteSpace(input) || config == null)
            {
                return null;

            }

            foreach (var type in config.MaterialTypes)
            {
                foreach (var sub in type.SubTypes)
                {
                    if (!string.IsNullOrEmpty(sub.SubTypeName) && input.Contains(sub.SubTypeName))
                    {
                        // 将 string 转换为 int 进行数值比较
                        if (int.TryParse(type.TypeName, out int currentTypeNameInt))
                        {
                            if (currentTypeNameInt > maxVal)
                            {
                                maxVal = currentTypeNameInt;
                            }
                            found = true;
                        }

                        break;
                    }
                }
            }

            return maxVal.ToString();
        }
    }
}
