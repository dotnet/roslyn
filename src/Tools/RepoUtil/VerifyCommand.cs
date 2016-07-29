using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    /// <summary>
    /// This utility is used to verify the repo is in a consistent state with respect to NuGet references. 
    /// </summary>
    internal sealed class VerifyCommand : ICommand
    {
        private struct NuGetPackageSource
        {
            internal NuGetPackage NuGetPackage { get; }
            internal FileName FileName { get; }

            internal NuGetPackageSource(NuGetPackage package, FileName fileName)
            {
                NuGetPackage = package;
                FileName = fileName;
            }
        }

        private readonly string _sourcesPath;
        private readonly RepoConfig _repoConfig;

        internal VerifyCommand(RepoConfig repoConfig, string sourcesPath)
        {
            _repoConfig = repoConfig;
            _sourcesPath = sourcesPath;
        }

        public bool Run(TextWriter writer, string[] args)
        {
            return
                VerifyProjectJsonContents(writer) &&
                VerifyRepoConfig(writer);
        }

        /// <summary>
        /// Verify the packages listed in project.json are well formed.  Packages should all either have the same version or 
        /// be explicitly fixed in the config file.
        /// </summary>
        private bool VerifyProjectJsonContents(TextWriter writer)
        {
            writer.WriteLine($"Verifying project.json contents");
            var allGood = true;
            var staticPackageSet = new HashSet<NuGetPackage>(_repoConfig.FixedPackages);
            var floatingPackageMap = new Dictionary<string, NuGetPackageSource>(Constants.NugetPackageNameComparer);
            foreach (var filePath in ProjectJsonUtil.GetProjectJsonFiles(_sourcesPath))
            {
                var fileName = FileName.FromFullPath(_sourcesPath, filePath);
                foreach (var package in ProjectJsonUtil.GetDependencies(filePath))
                {
                    if (staticPackageSet.Contains(package))
                    {
                        continue;
                    }

                    NuGetPackageSource source;

                    // If this is the first time we've seen the package then record where it was found.  Need the source
                    // information to provide better error messages later.
                    if (!floatingPackageMap.TryGetValue(package.Name, out source))
                    {
                        source = new NuGetPackageSource(package, fileName);
                        floatingPackageMap.Add(package.Name, source);
                        continue;
                    }

                    if (source.NuGetPackage != package)
                    {
                        writer.WriteLine($"Error! Package {package.Name} has different versions:");
                        writer.WriteLine($"\t{fileName} at {package.Version}");
                        writer.WriteLine($"\t{source.FileName} at {source.NuGetPackage.Version}");
                        writer.WriteLine($"The versions must be the same or one must be explicitly listed as fixed in RepoData.json");
                        allGood = false;
                    }
                }
            }

            return allGood;
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
    }
}
