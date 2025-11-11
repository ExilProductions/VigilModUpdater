using System;
using System.Collections.Generic;
using System.Text;

namespace VigilModUpdater.Models
{
    internal class ModUpdateInfo
    {
        public string ModName { get; set; } = "";
        public string LocalPath { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string NewVersion { get; set; } = "";
    }
}
