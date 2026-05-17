using System.Collections.Generic;

namespace GHPHandShake.Models
{
    public class MaterialUploadHistoryStore
    {
        public List<MaterialUploadLogEntry> Entries { get; set; } = new List<MaterialUploadLogEntry>();
    }
}
