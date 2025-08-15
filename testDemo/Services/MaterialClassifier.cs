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
            if (string.IsNullOrWhiteSpace(input) || config == null)
            {
                return null;

            }

            foreach (var type in config.MaterialTypes)
            {
                foreach (var sub in type.SubTypes)
                {
                    if (!string.IsNullOrEmpty(sub.Name) && input.Contains(sub.Name))
                    {
                        return type.TypeName;
                    }
                }
            }

            return null;
        }
    }
}
