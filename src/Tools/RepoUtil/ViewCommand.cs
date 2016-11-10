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

        /// <summary>
        /// Verify the packages listed in project.json are well formed.  Packages should all either have the same version or 
        /// be explicitly fixed in the config file.
        /// </summary>
        private bool VerifyProjectJsonContents(TextWriter writer, out RepoData repoData)
        {
            writer.WriteLine($"Verifying project.json contents");

            List<NuGetPackageConflict> conflicts;
            repoData = RepoData.Create(_repoConfig, _sourcesPath, out conflicts);
            if (conflicts?.Count > 0)
            { 
                foreach (var conflict in conflicts)
                {
                    writer.WriteLine($"Error! Package {conflict.PackageName} has different versions:");
                    writer.WriteLine($"\t{conflict.Original.FileName} at {conflict.Original.NuGetPackage.Version}");
                    writer.WriteLine($"\t{conflict.Conflict.FileName} at {conflict.Conflict.NuGetPackage.Version}");
                    writer.WriteLine($"The versions must be the same or one must be explicitly listed as fixed in RepoData.json");
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Verify that all of the data contained in the repo configuration is valid.  In particular that it hasn't gotten
        /// stale and referring to invalid packages.
        /// </summary>
        /// <param name="writer"></param>
        private bool VerifyRepoConfig(TextWriter writer)
        {
            writer.WriteLine($"Verifying RepoData.json");
            var packages = ProjectJsonUtil
                .GetProjectJsonFiles(_sourcesPath)
                .SelectMany(x => ProjectJsonUtil.GetDependencies(x));
            var set = new HashSet<NuGetPackage>(packages);
            var allGood = true;

            foreach (var package in _repoConfig.FixedPackages)
            {
                if (!set.Contains(package))
                {
                    writer.WriteLine($"Error: Fixed package {package.Name} - {package.Version} is not used anywhere");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool VerifyGeneratedFiles(TextWriter writer, RepoData repoData)
        {
            var allGood = true;
            writer.WriteLine($"Verifying generated files");
            if (_repoConfig.MSBuildGenerateData.HasValue)
            {
                var data = _repoConfig.MSBuildGenerateData.Value;
                var packages = GenerateUtil.GetFilteredPackages(data, repoData);

                // Need to verify the contents of the generated file are correct.
                var fileName = new FileName(_sourcesPath, data.RelativeFileName);
                var actualContent = File.ReadAllText(fileName.FullPath, GenerateUtil.Encoding);
                var expectedContent = GenerateUtil.GenerateMSBuildContent(packages);
                if (actualContent != expectedContent)
                {
                    writer.WriteLine($"{fileName.RelativePath} does not have the expected contents");
                    allGood = false;
                }

                if (!allGood)
                {
                    writer.WriteLine($@"Generated contents out of date. Run ""RepoUtil.change"" to correct");
                    return false;
                }

                // Verify none of the regex entries are stale.
                var staleRegexList = GenerateUtil.GetStaleRegex(data, repoData);
                foreach (var regex in staleRegexList)
                {
                    writer.WriteLine($"Regex {regex} matches no packages");
                    allGood = false;
                }
            }

            return allGood;
        }
    }
}
