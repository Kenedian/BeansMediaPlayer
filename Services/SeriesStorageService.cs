using BeansMediaPlayer.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace BeansMediaPlayer.Services
{
    public class SeriesStorageService
    {
        private readonly string _seriesFolderPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Series");

        public SeriesStorageService()
        {
            Directory.CreateDirectory(_seriesFolderPath);
        }

        public void SaveSeries(ImportedSeries series)
        {
            string filePath = Path.Combine(
                _seriesFolderPath,
                $"{series.Name}.txt");

            var lines = new List<string>
            {
                $"Path={series.FolderPath}",
                $"Volume={series.Volume}"
            };

            if (series.Resume is not null && series.Resume.HasResume)
            {
                lines.Add($"HasResume=True");
                lines.Add($"ResumeSeason={series.Resume.Season}");
                lines.Add($"ResumeEpisode={series.Resume.Episode}");
                lines.Add($"ResumePosition={series.Resume.Position:hh\\:mm\\:ss}");
            }

            File.WriteAllLines(filePath, lines);
        }

        public void DeleteSeries(ImportedSeries series)
        {
            string filePath = Path.Combine(
                _seriesFolderPath,
                $"{series.Name}.txt");

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public List<ImportedSeries> LoadAllSeriesMetadata()
        {
            List<ImportedSeries> result = new();

            var files = Directory.GetFiles(_seriesFolderPath, "*.txt");

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);

                var series = new ImportedSeries();
                var resume = new ResumeData();

                bool hasResume = false;

                foreach (var line in lines)
                {
                    if (line.StartsWith("Path="))
                        series.FolderPath = line.Substring("Path=".Length);

                    else if (line.StartsWith("Volume="))
                        series.Volume = int.Parse(line.Substring("Volume=".Length));

                    else if (line.StartsWith("HasResume="))
                        hasResume = bool.Parse(line.Substring("HasResume=".Length));

                    else if (line.StartsWith("ResumeSeason="))
                        resume.Season = int.Parse(line.Substring("ResumeSeason=".Length));

                    else if (line.StartsWith("ResumeEpisode="))
                        resume.Episode = int.Parse(line.Substring("ResumeEpisode=".Length));

                    else if (line.StartsWith("ResumePosition="))
                        resume.Position = TimeSpan.Parse(
                            line.Substring("ResumePosition=".Length));
                }

                if (hasResume)
                {
                    resume.HasResume = true;
                    series.Resume = resume;
                }

                result.Add(series);
            }

            return result;
        }
    }
}