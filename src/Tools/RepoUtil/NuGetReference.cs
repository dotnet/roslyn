using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal struct NuGetReference
    {
        internal string Name { get; }
        internal string Version { get; }

        internal NuGetReference(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
