using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RepoUtil
{
    /// <summary>
    /// The repo tool will generate include files, props, etc ... that containt NuGet versions.  This struct contains
    /// information about where to generate and what packages should match.
    /// </summary>
    internal struct GenerateData
    {
        internal string RelativeFileName { get; }
        internal ImmutableArray<Regex> Packages { get; }

        internal GenerateData(string relativeFileName, ImmutableArray<Regex> packages)
        {
            RelativeFileName = relativeFileName;
            Packages = packages;
        }
    }
}
