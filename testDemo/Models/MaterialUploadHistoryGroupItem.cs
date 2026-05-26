namespace GHPHandShake.Models
{
    /// <summary>按 项目 + Pos + 物料号 聚合后的展示项。</summary>
    public class MaterialUploadHistoryGroupItem
    {
        public string ProjectName { get; set; }
        public string TypeName { get; set; }
        public string SubTypeName { get; set; }
        public string MachineName { get; set; }
        /// <summary>最近一次成功上料的完整 SU（扫码内容）。</summary>
        public string LatestScanContent { get; set; }
        /// <summary>主表摘要显示（截断）。</summary>
        public string LatestScanPreview { get; set; }
        public int UploadCount { get; set; }
        public string LastTime { get; set; }
    }
}
