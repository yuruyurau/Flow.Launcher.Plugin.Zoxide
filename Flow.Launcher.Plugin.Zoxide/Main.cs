using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin;
namespace Flow.Launcher.Plugin.Zoxide
{
    public class Main : IPlugin
    {
        private PluginInitContext _context; public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            var search = query.Search?.Trim() ?? "";
            var paths = RunZoxide(search);

            if (paths.Count == 0)
            {
                return new List<Result>
            {
                new Result
                {
                    Title = string.IsNullOrEmpty(search) ? "zoxide has no history" : "No matches",
                    SubTitle = string.IsNullOrEmpty(search)
                        ? "Visit directories with `z` or run `zoxide add`"
                        : $"Search: {search}",
                    IcoPath = "assets/folder.png",
                }
            };
            }

            var lastKeyword = string.IsNullOrWhiteSpace(search)
                ? ""
                : search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";

            return paths.Select((path, index) =>
            {
                var title = GetDisplayTitle(path);
                var subtitle = FormatPath(path);

                var titleMatch = string.IsNullOrWhiteSpace(lastKeyword)
                    ? null
                    : _context.API.FuzzySearch(lastKeyword, title);

                var score = 1000 - index * 5 + (titleMatch?.Score ?? 0);

                return new Result
                {
                    Title = title,
                    SubTitle = subtitle,
                    IcoPath = "assets/folder.png",
                    Score = score,
                    TitleHighlightData = titleMatch?.MatchData,
                    Action = _ =>
                    {
                        _context.API.OpenDirectory(path);
                        return true;
                    }
                };
            }).ToList();

        }

        private string GetDisplayTitle(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            var normalized = path.Replace('\\', '/').TrimEnd('/');

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                        .Replace('\\', '/')
                                        .TrimEnd('/');

            var msysHome = $"C:/msys64/home/{Environment.UserName}".TrimEnd('/');

            if (normalized.Equals(userProfile, StringComparison.OrdinalIgnoreCase))
                return "~";

            if (normalized.Equals(msysHome, StringComparison.OrdinalIgnoreCase))
                return "~msys";

            return Path.GetFileName(path.TrimEnd('\\', '/'));
        }

        private string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            var normalized = path.Replace('\\', '/');

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                        .Replace('\\', '/');

            if (normalized.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            {
                return "~" + normalized[userProfile.Length..];
            }

            var msysHome = $"C:/msys64/home/{Environment.UserName}";
            if (normalized.StartsWith(msysHome, StringComparison.OrdinalIgnoreCase))
            {
                return "~msys" + normalized[msysHome.Length..];
            }

            return path;
        }

        private List<string> RunZoxide(string search)
        {
            try
            {
                var normalizedSearch = string.IsNullOrWhiteSpace(search)
                    ? ""
                    : search.Replace('/', '\\');

                var args = string.IsNullOrWhiteSpace(normalizedSearch)
                    ? "query -l"
                    : $"query -l {normalizedSearch}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "zoxide",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return new List<string>();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                return output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Take(50)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
