using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RepoUtil
{
    /// <summary>
    /// This utility is used to verify the repo is in a consistent state with respect to NuGet references. 
    /// </summary>
    internal sealed class ViewCommand : ICommand
    {
        private readonly string _sourcesPath;
        private readonly RepoConfig _repoConfig;

        internal ViewCommand(RepoConfig repoConfig, string sourcesPath)
        {
            _repoConfig = repoConfig;
            _sourcesPath = sourcesPath;
        }

        public bool Run(TextWriter writer, string[] args)
        {
            var list = args
                .Select(x => new Regex(x))
                .ToList();
            if (list.Count == 0)
            {
                list.Add(new Regex(".*"));
            }

            var map = new Dictionary<NuGetPackage, List<FileName>>();
            foreach (var filePath in ProjectJsonUtil.GetProjectJsonFiles(_sourcesPath))
            {
                var fileName = FileName.FromFullPath(_sourcesPath, filePath);
                foreach (var package in ProjectJsonUtil.GetDependencies(fileName.FullPath))
                {
                    if (list.All(x => !x.IsMatch(package.Name)))
                    {
                        continue;
                    }

                    List<FileName> nameList;
                    if (!map.TryGetValue(package, out nameList))
                    {
                        nameList = new List<FileName>();
                        map[package] = nameList;
                    }

                    nameList.Add(fileName);
                }
            }

            foreach (var pair in map.OrderBy(x => x.Key.Name))
            {
                var package = pair.Key;
                writer.WriteLine($"{package.Name} - {package.Version}");
                foreach (var fileName in pair.Value)
                {
                    writer.WriteLine($"\t{fileName.RelativePath}");
                }
            }

            return true;
        }
    }
}
