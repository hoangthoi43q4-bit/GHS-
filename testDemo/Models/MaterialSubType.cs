using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHPHandShake.Models
{
    public class MaterialSubType
    {
        public string SubTypeName { get; set; }

        /// <summary>
        /// The name of the machine this material is associated with.
        /// This must match an EquipmentId from your MachineInfo class.
        /// </summary>
        public string AssociatedMachineName { get; set; }
    }
}
