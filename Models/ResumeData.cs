using System;
using System.Collections.Generic;
using System.Text;

namespace BeansMediaPlayer.Models
{
    public class ResumeData
    {
        public bool HasResume { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }
        public TimeSpan Position { get; set; }
    }
}
