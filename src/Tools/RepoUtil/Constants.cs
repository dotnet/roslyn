using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal static class Constants
    {
        /// <summary>
        /// NuGet package names are not case sensitive.
        /// </summary>
        internal static readonly StringComparer NugetPackageNameComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// NuGet package versions case sensitivity is not documented anywhere that could be found.  Assuming
        /// case insensitive for now.
        /// </summary>
        internal static readonly StringComparer NugetPackageVersionComparer = StringComparer.OrdinalIgnoreCase;
    }
}
