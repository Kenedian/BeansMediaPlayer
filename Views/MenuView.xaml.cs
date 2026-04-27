using BeansMediaPlayer.Models;
using BeansMediaPlayer.Services;
using BeansMediaPlayer.Views;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace BeansMediaPlayer
{
    public partial class MenuView : System.Windows.Controls.UserControl
    {
        private readonly SeriesImportService _importService = new();
        private readonly SeriesStorageService _storageService = new();

        public MenuView()
        {
            InitializeComponent();

            LoadSavedSeries();
        }

        private void ImportSeries_Click(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new();

            dialog.Description = "Select Series Folder";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ImportedSeries importedSeries =
                    _importService.ImportSeries(dialog.SelectedPath);

                if (SeriesListBox.Items.Cast<ImportedSeries>()
                    .Any(x => x.FolderPath == importedSeries.FolderPath))
                {
                    System.Windows.MessageBox.Show("Series already imported.");
                    return;
                }

                _storageService.SaveSeries(importedSeries);

                SeriesListBox.Items.Add(importedSeries);
            }
        }

        private void LoadSavedSeries()
        {
            var savedSeries = _storageService.LoadAllSeriesMetadata();

            foreach (var series in savedSeries)
            {
                if (!System.IO.Directory.Exists(series.FolderPath))
                    continue;

                var importedSeries = _importService.ImportSeries(series.FolderPath);

                importedSeries.Resume = series.Resume;
                importedSeries.Volume = series.Volume;

                if (SeriesListBox.Items.Cast<ImportedSeries>()
                    .Any(x => x.FolderPath == importedSeries.FolderPath))
                    continue;

                SeriesListBox.Items.Add(importedSeries);
            }
        }

        private void SeriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SeasonsPanel.Children.Clear();

            if (SeriesListBox.SelectedItem is not ImportedSeries selectedSeries)
            {
                ResumeButton.IsEnabled = false;
                ResumeButton.Content = "Resume";
                return;
            }

            UpdateResumeButton(selectedSeries);

            foreach (var season in selectedSeries.Seasons)
            {
                var expander = new Expander
                {
                    Header = $"{season.Number}. Series",
                    Foreground = System.Windows.Media.Brushes.White,
                    Background = (System.Windows.Media.Brush)FindResource("BgSecondary"),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var episodePanel = new StackPanel();

                foreach (var episode in season.Episodes)
                {
                    var button = new System.Windows.Controls.Button
                    {
                        Content = $"Episode {episode.Number}",
                        Tag = episode,
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    button.Click += EpisodeButton_Click;

                    episodePanel.Children.Add(button);
                }

                expander.Content = episodePanel;

                SeasonsPanel.Children.Add(expander);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (SeriesListBox.SelectedItem is not ImportedSeries selectedSeries)
                return;

            if (selectedSeries.Resume is null || !selectedSeries.Resume.HasResume)
                return;

            var resume = selectedSeries.Resume;

            var season = selectedSeries.Seasons
                .FirstOrDefault(s => s.Number == resume.Season);

            if (season is null)
                return;

            var episode = season.Episodes
                .FirstOrDefault(e => e.Number == resume.Episode);

            if (episode is null)
                return;

            ((MainWindow)System.Windows.Application.Current.MainWindow)
                .MainContent.Content = new PlayerView(selectedSeries,
                    episode,
                    resume.Position);
        }

        private void EpisodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn &&
                btn.Tag is Episode episode &&
                SeriesListBox.SelectedItem is ImportedSeries series)
            {
                ((MainWindow)System.Windows.Application.Current.MainWindow)
                    .MainContent.Content =
                        new PlayerView(series, episode);
            }
        }

        private void SeriesListBox_RightClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;

            while (source != null && source is not ListBoxItem)
                source = VisualTreeHelper.GetParent(source);

            if (source is not ListBoxItem item)
                return;

            item.IsSelected = true;

            if (item.DataContext is not ImportedSeries series)
                return;

            var result = System.Windows.MessageBox.Show(
                $"Remove '{series.Name}' from Beans Media Player?\n\nVideo files will NOT be deleted.",
                "Delete Series",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            _storageService.DeleteSeries(series);

            SeriesListBox.Items.Remove(series);

            SeasonsPanel.Children.Clear();
        }

        private void UpdateResumeButton(ImportedSeries selectedSeries)
        {
            if (selectedSeries.Resume is null || !selectedSeries.Resume.HasResume)
            {
                ResumeButton.IsEnabled = false;
                ResumeButton.Content = "Resume";
                return;
            }

            var resume = selectedSeries.Resume;

            ResumeButton.IsEnabled = true;
            ResumeButton.Content =
                $"Resume (S{resume.Season:00}:E{resume.Episode:00} - {resume.Position:hh\\:mm\\:ss})";
        }
    }
}