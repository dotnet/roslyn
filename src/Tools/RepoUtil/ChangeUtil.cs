using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    /// <summary>
    /// Responsible for changing the repo to use a new set of NuGet packages.
    /// </summary>
    internal sealed class ChangeUtil
    {
        private readonly RepoData _repoData;

        internal ChangeUtil(RepoData repoData)
        {
            _repoData = repoData;
        }

        internal void Go()
        {
            // TODO: actually take an URL
            var list = new List<NuGetPackage>();
            foreach (var line in File.ReadAllLines(@"e:\temp\test.txt"))
            {
                var item = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var package = new NuGetPackage(item[0], item[1]);
                list.Add(package);
            }

            Go(list);
        }

        /// <summary>
        /// Change the state of the repo to use the specified packages.
        /// </summary>
        internal void Go(IEnumerable<NuGetPackage> packages)
        {
            var changeList = CalculateChanges(packages);
            var map = ImmutableDictionary<string, NuGetPackage>.Empty.WithComparers(Constants.NugetPackageNameComparer);
            foreach (var package in changeList)
            {
                map = map.Add(package.Name, package.NewPackage);
            }
            GoCore(map);
        }

        private List<NuGetPackageChange> CalculateChanges(IEnumerable<NuGetPackage> packages)
        {
            var map = _repoData
                .FloatingPackages
                .ToDictionary(x => x.Name, Constants.NugetPackageNameComparer);
            var list = new List<NuGetPackageChange>();

            Console.WriteLine("Calculating the changes");
            foreach (var package in packages)
            {
                NuGetPackage existingPackage;
                if (!map.TryGetValue(package.Name, out existingPackage))
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
        private void GoCore(ImmutableDictionary<string, NuGetPackage> changeMap)
        {
            Console.WriteLine("Changing project.json files");
            foreach (var filePath in ProjectJsonUtil.GetProjectJsonFiles(_repoData.SourcesPath))
            {
                if (ProjectJsonUtil.ChangeDependencies(filePath, changeMap))
                {
                    Console.WriteLine($"\t{filePath} updated");
                }
            }

            Console.WriteLine("Generating files");
            var util = new GenerateUtil(_repoData);
            util.Go();
        }
    }
}
