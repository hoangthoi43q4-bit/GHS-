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
                        var successScans = ordered
                            .Where(x => x.IsAckSuccess && !string.IsNullOrWhiteSpace(x.ScanContent))
                            .Select(x => x.ScanContent.Trim())
                            .ToList();

                        string chain = successScans.Count == 0
                            ? "（暂无成功上料记录）"
                            : string.Join(" -> ", successScans);

                        var last = ordered.Last();
                        return new MaterialUploadHistoryGroupItem
                        {
                            ProjectName = g.Key.Project,
                            TypeName = g.Key.Type,
                            SubTypeName = g.Key.Sub,
                            MachineName = last.MachineName,
                            SuChain = chain,
                            UploadCount = successScans.Count,
                            LastTime = last.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    })
                    .OrderBy(x => x.ProjectName)
                    .ThenBy(x => x.TypeName)
                    .ThenBy(x => x.SubTypeName)
                    .ToList();
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
    }
}
