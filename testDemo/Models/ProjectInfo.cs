using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHPHandShake.Models
{
    /// <summary>
    /// 第一层：项目信息 (对应 Excel 第四列)
    /// </summary>
    public class ProjectInfo
    {
        public string ProjectName { get; set; }

        // 每个项目下包含多个位置/类型
        public ObservableCollection<MaterialType> MaterialTypes { get; set; }

        public ProjectInfo()
        {
            MaterialTypes = new ObservableCollection<MaterialType>();
        }
    }
}
