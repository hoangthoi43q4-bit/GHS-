using System;

namespace GHPHandShake.Models
{
    public class MaterialUploadLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string ProjectName { get; set; }
        /// <summary>类型/Pos，对应配置中的 TypeName。</summary>
        public string TypeName { get; set; }
        public string SubTypeName { get; set; }
        public string MachineName { get; set; }
        /// <summary>扫码传入的完整内容（SU）。</summary>
        public string ScanContent { get; set; }
        public bool IsAckSuccess { get; set; }
        public string Response { get; set; }
    }
}
