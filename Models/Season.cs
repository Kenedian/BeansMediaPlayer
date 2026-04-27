using System;
using System.Collections.Generic;
using System.Text;

namespace BeansMediaPlayer.Models
{
    public class Season
    {
        public int Number { get; set; }
        public List<Episode> Episodes { get; set; } = new();
    }
}
