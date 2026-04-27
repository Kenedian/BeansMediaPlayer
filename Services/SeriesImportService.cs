using BeansMediaPlayer.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace BeansMediaPlayer.Services
{
    public class SeriesImportService
    {
        public ImportedSeries ImportSeries(string folderPath)
        {
            var series = new ImportedSeries
            {
                Name = Path.GetFileName(folderPath),
                FolderPath = folderPath
            };

            var seasonDirs = Directory.GetDirectories(folderPath);

            foreach (var seasonDir in seasonDirs)
            {
                string seasonFolderName = Path.GetFileName(seasonDir);

                Match seasonMatch = Regex.Match(seasonFolderName, @"s\s*\((\d+)\)", RegexOptions.IgnoreCase);

                if (!seasonMatch.Success)
                    continue;

                int seasonNumber = int.Parse(seasonMatch.Groups[1].Value);

                var season = new Season
                {
                    Number = seasonNumber
                };

                var episodeFiles = Directory.GetFiles(seasonDir);

                foreach (var episodeFile in episodeFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(episodeFile);

                    Match episodeMatch = Regex.Match(fileName, @"e\s*\((\d+)\)", RegexOptions.IgnoreCase);

                    if (!episodeMatch.Success)
                        continue;

                    int episodeNumber = int.Parse(episodeMatch.Groups[1].Value);

                    season.Episodes.Add(new Episode
                    {
                        Number = episodeNumber,
                        FilePath = episodeFile
                    });
                }

                season.Episodes = season.Episodes
                    .OrderBy(e => e.Number)
                    .ToList();

                series.Seasons.Add(season);
            }

            series.Seasons = series.Seasons
                .OrderBy(s => s.Number)
                .ToList();

            return series;
        }
    }
}