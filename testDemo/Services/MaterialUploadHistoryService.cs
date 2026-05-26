using GHPHandShake.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GHPHandShake.Services
{
    public class MaterialUploadHistoryService
    {
        private const string HistoryFilePath = "upload_history.json";
        private readonly object _syncRoot = new object();
        private MaterialUploadHistoryStore _store = new MaterialUploadHistoryStore();

        public MaterialUploadHistoryService()
        {
            Load();
        }

        public void Load()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(HistoryFilePath))
                {
                    _store = new MaterialUploadHistoryStore();
                    return;
                }

                try
                {
                    string json = File.ReadAllText(HistoryFilePath);
                    _store = JsonConvert.DeserializeObject<MaterialUploadHistoryStore>(json)
                             ?? new MaterialUploadHistoryStore();
                    if (_store.Entries == null)
                    {
                        _store.Entries = new List<MaterialUploadLogEntry>();
                    }
                }
                catch
                {
                    _store = new MaterialUploadHistoryStore();
                }
            }
        }

        public void AddEntry(MaterialUploadLogEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                _store.Entries.Add(entry);
                SaveInternal();
            }
        }

        public IList<MaterialUploadHistoryGroupItem> GetGroupedHistory(string projectFilter)
        {
            lock (_syncRoot)
            {
                IEnumerable<MaterialUploadLogEntry> query = _store.Entries;
                if (!string.IsNullOrWhiteSpace(projectFilter))
                {
                    query = query.Where(e =>
                        string.Equals(e.ProjectName, projectFilter, StringComparison.OrdinalIgnoreCase));
                }

                return query
                    .GroupBy(e => new
                    {
                        Project = e.ProjectName ?? string.Empty,
                        Type = e.TypeName ?? string.Empty,
                        Sub = e.SubTypeName ?? string.Empty
                    })
                    .Select(g =>
                    {
                        var ordered = g.OrderBy(x => x.Timestamp).ToList();
                        var successEntries = ordered
                            .Where(x => x.IsAckSuccess && !string.IsNullOrWhiteSpace(x.ScanContent))
                            .ToList();

                        var lastSuccess = successEntries.LastOrDefault();
                        var last = ordered.Last();
                        string latestFull = lastSuccess?.ScanContent?.Trim() ?? string.Empty;

                        return new MaterialUploadHistoryGroupItem
                        {
                            ProjectName = g.Key.Project,
                            TypeName = g.Key.Type,
                            SubTypeName = g.Key.Sub,
                            MachineName = last.MachineName,
                            LatestScanContent = latestFull,
                            LatestScanPreview = string.IsNullOrEmpty(latestFull)
                                ? "（暂无成功上料）"
                                : TruncatePreview(latestFull),
                            UploadCount = successEntries.Count,
                            LastTime = last.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    })
                    .OrderBy(x => x.ProjectName)
                    .ThenBy(x => x.TypeName)
                    .ThenBy(x => x.SubTypeName)
                    .ToList();
            }
        }

        public IList<MaterialUploadHistoryDetailItem> GetDetailHistory(
            string projectName,
            string typeName,
            string subTypeName)
        {
            lock (_syncRoot)
            {
                var entries = _store.Entries
                    .Where(e =>
                        string.Equals(e.ProjectName ?? string.Empty, projectName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(e.TypeName ?? string.Empty, typeName ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(e.SubTypeName ?? string.Empty, subTypeName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                var details = new List<MaterialUploadHistoryDetailItem>();
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    details.Add(new MaterialUploadHistoryDetailItem
                    {
                        Index = i + 1,
                        Timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        ScanContent = entry.ScanContent ?? string.Empty,
                        ResultText = entry.IsAckSuccess ? "ACK成功" : "未成功",
                        IsAckSuccess = entry.IsAckSuccess
                    });
                }

                return details;
            }
        }

        public IList<string> GetProjectNames()
        {
            lock (_syncRoot)
            {
                return _store.Entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.ProjectName))
                    .Select(e => e.ProjectName.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p)
                    .ToList();
            }
        }

        private void SaveInternal()
        {
            string json = JsonConvert.SerializeObject(_store, Formatting.Indented);
            File.WriteAllText(HistoryFilePath, json);
        }

        private static string TruncatePreview(string text, int maxLength = 48)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }
    }
}
