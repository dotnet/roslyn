using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Roslyn.MSBuild.Util
{
    /// <summary>
    /// This task will filter down the elements in the ReferenceLocalCopyPaths item group to 
    /// just the items in the set of NuGet packages we are interested in.
    /// </summary>
    public sealed class FindNuGetAssetsForVsix : Task
    {
        [Required]
        public string NuGetPackageRoot { get; set; }

        [Required]
        public ITaskItem[] ReferenceCopyLocalPaths { get; set; }

        [Required]
        public ITaskItem[] NuGetPackageToIncludeInVsix { get; set; }

        [Output]
        public ITaskItem[] NuGetAssetsToIncludeInVsix { get; set; }

        public FindNuGetAssetsForVsix()
        {

        }

        public override bool Execute()
        {
            var nugetDirs = new List<string>();
            foreach (var item in NuGetPackageToIncludeInVsix)
            {
                var nugetDir = NormalizePath(Path.Combine(NuGetPackageRoot, item.ItemSpec));
                nugetDirs.Add(nugetDir);
            }

            var assets = new List<ITaskItem>();
            foreach (var path in ReferenceCopyLocalPaths)
            {
                var itemPath = NormalizePath(path.ItemSpec);
                if (nugetDirs.Any(x => itemPath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    assets.Add(path);
                }
            }

            NuGetAssetsToIncludeInVsix = assets.ToArray();
            return true;
        }

        private static string NormalizePath(string path) => path.Replace('/', '\\').TrimEnd('\\');
    }
}
