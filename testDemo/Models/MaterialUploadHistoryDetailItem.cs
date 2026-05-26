namespace GHPHandShake.Models
{
    public class MaterialUploadHistoryDetailItem
    {
        public int Index { get; set; }
        public string Timestamp { get; set; }
        public string ScanContent { get; set; }
        public string ResultText { get; set; }
        public bool IsAckSuccess { get; set; }
    }
}
