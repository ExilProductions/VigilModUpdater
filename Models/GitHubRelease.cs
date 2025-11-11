using System;
using System.Collections.Generic;
using System.Text;

namespace VigilModUpdater.Models
{
    internal class GitHubRelease
    {
        public string TagName { get; set; } = "";
        public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
    }
}
