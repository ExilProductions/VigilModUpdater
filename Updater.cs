using System.Xml.Linq;
using MelonLoader;
using VigilModUpdater.Models;
using Newtonsoft.Json;

namespace VigilModUpdater
{
    internal class Updater : IDisposable
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string CacheFile = "VigilModUpdaterCache.xml";
        private bool _updatesPending = false;

        public Updater()
        {
            ApplyUpdates();
            MelonMod.OnMelonRegistered.Subscribe(OnModRegistered);
        }

        private void ApplyUpdates()
        {
            if (!File.Exists(CacheFile))
                return;

            try
            {
                var doc = XDocument.Load(CacheFile);
                var mods = doc.Root?.Elements("Mod").ToArray();
                if (mods == null || mods.Length == 0) return;

                foreach (var modElem in mods)
                {
                    string localPath = modElem.Element("LocalPath")?.Value ?? "";
                    string downloadUrl = modElem.Element("DownloadUrl")?.Value ?? "";
                    string modName = modElem.Element("ModName")?.Value ?? "";
                    string version = modElem.Element("NewVersion")?.Value ?? "";

                    if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(downloadUrl))
                        continue;

                    Core.Logger.Msg($"Applying cached update for {modName} ({version})...");

                    try
                    {
                        using var res = httpClient.GetAsync(downloadUrl).Result;
                        if (!res.IsSuccessStatusCode)
                        {
                            Core.Logger.Warning($"Failed to download {modName} from {downloadUrl}");
                            continue;
                        }

                        byte[] data = res.Content.ReadAsByteArrayAsync().Result;
                        string backup = localPath + ".backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        File.Move(localPath, backup);
                        File.WriteAllBytes(localPath, data);

                        Core.Logger.Msg($"Updated {modName} to {version}, backup saved at {backup}");
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.Error($"Failed to apply cached update for {modName}: {ex}");
                    }
                }
                File.Delete(CacheFile);
            }
            catch (Exception ex)
            {
                Core.Logger.Error("Failed to process cached updates: " + ex);
            }
        }

        private void OnModRegistered(MelonBase mod)
        {
            Core.Logger.Msg($"Checking for updates for mod: {mod.Info.Name}");
            if (string.IsNullOrEmpty(mod.Info.DownloadLink) || !mod.Info.DownloadLink.Contains("github.com"))
            {
                Core.Logger.Msg($"[{mod.Info.Name}] No valid GitHub link, skipping update check.");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var update = await CheckUpdate(mod);
                    if (update != null)
                    {
                        CacheUpdate(update);
                        _updatesPending = true;
                        Core.Logger.Msg($"[{mod.Info.Name}] Update found: {update.NewVersion}");
                    }
                    else
                    {
                        Core.Logger.Msg($"No updates available for {mod.Info.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    Core.Logger.Error($"[{mod.Info.Name}] Update check failed: {ex}");
                }
                finally
                {
                    if (_updatesPending)
                    {
                        Core.Logger.Msg("Updates pending. Restarting game to apply updates...");
                        RestartGame();
                    }
                }
            }).Wait();
        }

        private async Task<ModUpdateInfo?> CheckUpdate(MelonBase mod)
        {
            string repoApi = ConvertToApiUrl(mod.Info.DownloadLink);
            if (repoApi == null) return null;

            using var req = new HttpRequestMessage(HttpMethod.Get, repoApi + "/latest");
            req.Headers.UserAgent.ParseAdd("VigilModUpdater");
            var res = await httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode) return null;

            string json = await res.Content.ReadAsStringAsync();
            var release = JsonConvert.DeserializeObject<GitHubRelease>(json);
            if (release == null) return null;

            if (!Version.TryParse(mod.Info.Version, out var localVersion)) return null;
            if (!Version.TryParse(release.TagName.TrimStart('v', 'V'), out var remoteVersion)) return null;

            if (remoteVersion > localVersion)
            {
                string localPath = mod.MelonAssembly.Location;
                string dllName = Path.GetFileName(localPath);

                var asset = release.Assets.FirstOrDefault(a => a.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase));
                if (asset == null) return null;

                return new ModUpdateInfo
                {
                    ModName = mod.Info.Name,
                    LocalPath = localPath,
                    DownloadUrl = asset.DownloadUrl,
                    NewVersion = release.TagName
                };
            }

            return null;
        }

        private void CacheUpdate(ModUpdateInfo update)
        {
            XDocument doc;
            if (File.Exists(CacheFile))
            {
                doc = XDocument.Load(CacheFile);
            }
            else
            {
                doc = new XDocument(new XElement("Updates"));
            }

            var root = doc.Element("Updates");
            if (root == null) return;

            if (root.Elements("Mod").Any(m => m.Element("ModName")?.Value == update.ModName))
                return;

            var modElem = new XElement("Mod",
                new XElement("ModName", update.ModName),
                new XElement("LocalPath", update.LocalPath),
                new XElement("DownloadUrl", update.DownloadUrl),
                new XElement("NewVersion", update.NewVersion)
            );

            root.Add(modElem);
            doc.Save(CacheFile);
        }

        private string? ConvertToApiUrl(string downloadLink)
        {
            try
            {
                var uri = new Uri(downloadLink);
                if (uri.Host != "github.com") return null;
                var parts = uri.AbsolutePath.Trim('/').Split('/');
                if (parts.Length >= 3)
                    return $"https://api.github.com/repos/{parts[0]}/{parts[1]}/releases";
            }
            catch { }
            return null;
        }

        private void RestartGame()
        {
            try
            {
                var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                System.Diagnostics.Process.Start(exe);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Core.Logger.Error("Failed to restart game: " + ex);
            }
        }

        public void Dispose()
        {
            MelonMod.OnMelonRegistered.Unsubscribe(OnModRegistered);
        }
    }
}
