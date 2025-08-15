using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHPHandShake.Models
{
    public class MaterialType
    {
        public string TypeName { get; set; }
        public List<MaterialSubType> SubTypes { get; set; } = new List<MaterialSubType>();
    }
}
