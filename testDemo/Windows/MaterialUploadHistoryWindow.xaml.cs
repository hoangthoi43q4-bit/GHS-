using GHPHandShake.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
