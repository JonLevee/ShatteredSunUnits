namespace GenerateUnitsJson.Utils
{
    public class SteamInfo
    {
        private readonly List<string> steamFolders = new List<string>();
        private readonly Dictionary<string, string> folderPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string GetRoot(string appName = "Sanctuary Shattered Sun Demo")
        {
            if (!steamFolders.Any())
            {
                var programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var steamLibraryFolderFile = Path.Combine(programFilesFolder, @"Steam\steamapps\libraryfolders.vdf");
                var pathKey = "\"path\"";
                var paths = File.ReadAllLines(steamLibraryFolderFile)
                    .Where(s => s.Contains(pathKey))
                    .Select(s => s.Trim().Substring(pathKey.Length).Trim(' ', '\"', '\t'))
                    .Select(s => s.Replace(@"\\", @"\"))
                    .ToList();
                steamFolders.AddRange(paths);
            }
            if (!folderPaths.ContainsKey(appName))
            {
                foreach (var steamFolder in steamFolders)
                {
                    var path = Path.Combine(steamFolder, @"steamapps\common", appName);
                    if (Directory.Exists(path))
                    {
                        folderPaths.Add(appName, path);
                    }
                }
            }

            var root = folderPaths[appName];

            return root;
        }
    }
}
