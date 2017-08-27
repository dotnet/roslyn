using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RepoUtil
{
    /// <summary>
    /// Responsible for changing the repo to use a new set of NuGet packages.
    /// </summary>
    internal sealed class ChangeCommand : ICommand
    {
        private readonly RepoData _repoData;
        private readonly string _generateDirectory;

        internal ChangeCommand(RepoData repoData, string generateDir)
        {
            _repoData = repoData;
            _generateDirectory = generateDir;
        }

        public bool Run(TextWriter writer, string[] args)
        {
            if (!TryParseChangeSource(writer, args, out var changes))
            {
                return false;
            }

            ChangeAll(changes);
            return true;
        }

        /// <summary>
        /// Parse out the arguments that can be provided to the 'change' command.
        /// </summary>
        private static bool TryParseChangeSource(TextWriter writer, string[] args, out List<NuGetPackage> changes)
        {
            changes = new List<NuGetPackage>();
            var allGood = true;
            var index = 0;
            while (index < args.Length && allGood)
            {
                var arg = args[index];
                index++;

                switch (arg.ToLower())
                {
                    case "-version":
                        if (index < args.Length)
                        {
                            allGood = TryParseVersionSource(writer, args[index], changes);
                            index++;
                        }
                        else
                        {
                            Console.WriteLine($"The -version switch needs a value");
                            allGood = false;
                        }
                        break;
                    default:
                        if (!TryParsePackage(writer, arg, changes))
                        {
                            allGood = false;
                        }
                        break;
                }
            }

            return allGood;
        }

        /// <summary>
        /// Parse out a file that has the format used on the dotnet/versions repo.  Essentially every entry in the file
        /// will be a package name, space, version.
        /// </summary>
        private static bool TryParseVersionSource(TextWriter writer, string path, List<NuGetPackage> changes)
        {
            try
            {
                var allGood = true;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (!TryParsePackage(writer, line, changes))
                    {
                        writer.WriteLine($"Error parsing {line}");
                        allGood = false;
                    }
                }

                return allGood;
            }
            catch (Exception ex)
            {
                writer.WriteLine($"Error parsing {path}: {ex.ToString()}");
                writer.WriteLine(ex.StackTrace);
                changes = null;
                return false;
            }
        }

        private static bool TryParsePackage(TextWriter writer, string packageLine, List<NuGetPackage> changes)
        {
            var match = Regex.Match(packageLine, @"([^\s]*)\s*(.*)");
            if (match.Success)
            {
                var package = new NuGetPackage(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
                changes.Add(package);
                return true;
            }

            match = Regex.Match(packageLine, @"([^\s]*)\s*-\s*(.*)");
            if (match.Success)
            {
                var package = new NuGetPackage(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
                changes.Add(package);
                return true;
            }

            writer.WriteLine($"Unable to parse package {packageLine}");
            return false;
        }

        /// <summary>
        /// Change the state of the repo to use the specified packages.
        /// </summary>
        internal void ChangeAll(IEnumerable<NuGetPackage> packages)
        {
            var changeList = CalculateChanges(packages);
            var map = ImmutableDictionary<NuGetPackage, NuGetPackage>.Empty;
            foreach (var package in changeList)
            {
                map = map.Add(package.OldPackage, package.NewPackage);
            }
            ChangeAllCore(map);
        }

        /// <summary>
        /// Produce the set of <see cref="NuGetPackageChange"/> values for the set of updated 
        /// nuget package references.
        /// </summary>
        private List<NuGetPackageChange> CalculateChanges(IEnumerable<NuGetPackage> packages)
        {
            var map = _repoData
                .FloatingPackages
                .ToDictionary(x => x.Name, Constants.NugetPackageNameComparer);
            var list = new List<NuGetPackageChange>();

            Console.WriteLine("Calculating the changes");
            foreach (var package in packages)
            {
                if (!map.TryGetValue(package.Name, out var existingPackage))
                {
                    Console.WriteLine($"\tSkipping {package.Name} as it's not a floating package in this repo.");
                }

                list.Add(new NuGetPackageChange(package.Name, oldVersion: existingPackage.Version, newVersion: package.Version));
            }

            return list;
        }

        /// <summary>
        /// Change the repo to respect the new version for the specified set of NuGet packages
        /// </summary>
        private void ChangeAllCore(ImmutableDictionary<NuGetPackage, NuGetPackage> changeMap)
        {
            ChangeProjectJsonFiles(changeMap);
            ChangeGeneratedFiles();
        }

        private void ChangeProjectJsonFiles(ImmutableDictionary<NuGetPackage, NuGetPackage> changeMap)
        {
            Console.WriteLine("Changing project.json files");
            foreach (var filePath in ProjectJsonUtil.GetProjectJsonFiles(_repoData.SourcesDirectory))
            {
                if (ProjectJsonUtil.ChangeDependencies(filePath, changeMap))
                {
                    Console.WriteLine($"\t{filePath} updated");
                }
            }
        }

        private void ChangeGeneratedFiles()
        {
            var msbuildData = _repoData.RepoConfig.MSBuildGenerateData;
            if (msbuildData.HasValue)
            {
                var fileName = new FileName(_generateDirectory, msbuildData.Value.RelativeFilePath);
                var packages = GenerateUtil.GetFilteredPackages(msbuildData.Value, _repoData);
                GenerateUtil.WriteMSBuildContent(fileName, packages);
            }
        }
    }
}
