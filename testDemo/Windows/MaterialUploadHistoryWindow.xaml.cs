using GHPHandShake.Models;
using GHPHandShake.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace GHPHandShake.Windows
{
    public partial class MaterialUploadHistoryWindow : Window
    {
        private readonly MaterialUploadHistoryService _historyService;
        private const string AllProjectsLabel = "（全部项目）";

        public MaterialUploadHistoryWindow(MaterialUploadHistoryService historyService, string defaultProject = null)
        {
            InitializeComponent();
            _historyService = historyService;
            LoadProjectFilter(defaultProject);
            RefreshGrid();
        }

        private void LoadProjectFilter(string defaultProject = null)
        {
            var projects = new List<string> { AllProjectsLabel };
            projects.AddRange(_historyService.GetProjectNames());
            ProjectFilterComboBox.ItemsSource = projects;

            if (!string.IsNullOrWhiteSpace(defaultProject) &&
                projects.Any(p => string.Equals(p, defaultProject, StringComparison.OrdinalIgnoreCase)))
            {
                ProjectFilterComboBox.SelectedItem = projects.First(p =>
                    string.Equals(p, defaultProject, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                ProjectFilterComboBox.SelectedIndex = 0;
            }
        }

        private void RefreshGrid()
        {
            string selected = ProjectFilterComboBox.SelectedItem as string;
            string filter = string.Equals(selected, AllProjectsLabel) ? null : selected;
            HistoryGrid.ItemsSource = _historyService.GetGroupedHistory(filter);
            DetailGrid.ItemsSource = null;
            DetailTitleText.Text = "上料明细（请选择上方记录）";
        }

        private void ProjectFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            RefreshGrid();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _historyService.Load();
            LoadProjectFilter();
            RefreshGrid();
        }

        private void HistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = HistoryGrid.SelectedItem as MaterialUploadHistoryGroupItem;
            if (selected == null)
            {
                DetailGrid.ItemsSource = null;
                DetailTitleText.Text = "上料明细（请选择上方记录）";
                return;
            }

            var details = _historyService.GetDetailHistory(
                selected.ProjectName,
                selected.TypeName,
                selected.SubTypeName);

            DetailGrid.ItemsSource = details;
            DetailTitleText.Text =
                $"上料明细：{selected.ProjectName} / Pos{selected.TypeName} / {selected.SubTypeName}（共 {details.Count} 条）";
        }

        private void CopySelectedDetail_Click(object sender, RoutedEventArgs e)
        {
            var selected = DetailGrid.SelectedItem as MaterialUploadHistoryDetailItem;
            if (selected == null || string.IsNullOrWhiteSpace(selected.ScanContent))
            {
                MessageBox.Show("请先在明细列表中选择一条记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CopyToClipboard(selected.ScanContent);
            MessageBox.Show("已复制选中 SU 到剪贴板。", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyAllDetails_Click(object sender, RoutedEventArgs e)
        {
            var group = HistoryGrid.SelectedItem as MaterialUploadHistoryGroupItem;
            if (group == null)
            {
                MessageBox.Show("请先在上方摘要表中选择一条记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var details = _historyService.GetDetailHistory(group.ProjectName, group.TypeName, group.SubTypeName);
            if (details.Count == 0)
            {
                MessageBox.Show("当前记录没有可复制的明细。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"项目: {group.ProjectName}");
            builder.AppendLine($"Pos: {group.TypeName}");
            builder.AppendLine($"物料号: {group.SubTypeName}");
            builder.AppendLine($"设备: {group.MachineName}");
            builder.AppendLine("---");

            foreach (var item in details)
            {
                builder.AppendLine($"#{item.Index}  {item.Timestamp}  [{item.ResultText}]");
                builder.AppendLine(item.ScanContent);
                builder.AppendLine();
            }

            CopyToClipboard(builder.ToString().TrimEnd());
            MessageBox.Show($"已复制全部 {details.Count} 条明细到剪贴板。", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyLatestSu_Click(object sender, RoutedEventArgs e)
        {
            var group = HistoryGrid.SelectedItem as MaterialUploadHistoryGroupItem;
            if (group == null)
            {
                MessageBox.Show("请先在上方摘要表中选择一条记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(group.LatestScanContent))
            {
                MessageBox.Show("当前记录暂无成功上料的 SU。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CopyToClipboard(group.LatestScanContent);
            MessageBox.Show("已复制最近成功 SU 到剪贴板。", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void CopyToClipboard(string text)
        {
            Clipboard.SetText(text ?? string.Empty);
        }
    }
}
