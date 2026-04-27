using System;
using System.Collections.Generic;
using System.Text;

namespace BeansMediaPlayer.Models
{
    public class ImportedSeries
    {
        public string Name { get; set; } = "";
        public string FolderPath { get; set; } = "";
        public List<Season> Seasons { get; set; } = new();
        public ResumeData? Resume { get; set; }
        public int Volume { get; set; } = 75;
    }
}
