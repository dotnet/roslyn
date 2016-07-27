using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal static class Constants
    {
        internal static readonly StringComparer NugetPackageNameComparer = StringComparer.OrdinalIgnoreCase;
        internal static readonly StringComparer NugetPackageVersionComparer = StringComparer.OrdinalIgnoreCase;
    }
}
