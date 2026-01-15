using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHPHandShake.Models
{
    public class MaterialConfig
    {
        //public List<MaterialType> MaterialTypes { get; set; } = new List<MaterialType>();

        /// <summary>
        /// The complete list of all material types.
        /// </summary>
        public ObservableCollection<MaterialType> MaterialTypes { get; set; }

        public MaterialConfig()
        {
            MaterialTypes = new ObservableCollection<MaterialType>();
        }
    }
}
