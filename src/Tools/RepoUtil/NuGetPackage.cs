using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal struct NuGetPackage
    {
        internal string Name { get; }
        internal string Version { get; }

        internal NuGetPackage(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
