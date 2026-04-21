using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHPHandShake.Models
{
    public class MaterialType
    {
        public string ProjectName { get; set; }
        public string TypeName { get; set; }
        //public List<MaterialSubType> SubTypes { get; set; } = new List<MaterialSubType>();

        /// <summary>
        /// A list of all specific materials belonging to this type.
        /// Using ObservableCollection allows the UI to update automatically.
        /// </summary>
        public ObservableCollection<MaterialSubType> SubTypes { get; set; }

        public MaterialType()
        {
            SubTypes = new ObservableCollection<MaterialSubType>();
        }
    }
}
