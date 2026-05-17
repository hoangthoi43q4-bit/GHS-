namespace GHPHandShake.Models
{
    /// <summary>按 项目 + Pos + 物料号 聚合后的展示项。</summary>
    public class MaterialUploadHistoryGroupItem
    {
        public string ProjectName { get; set; }
        public string TypeName { get; set; }
        public string SubTypeName { get; set; }
        public string MachineName { get; set; }
        public string SuChain { get; set; }
        public int UploadCount { get; set; }
        public string LastTime { get; set; }
    }
}
