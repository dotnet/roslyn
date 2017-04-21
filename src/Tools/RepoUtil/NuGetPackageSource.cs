using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal struct NuGetPackageSource
    {
        internal NuGetPackage NuGetPackage { get; }
        internal FileName FileName { get; }

        internal NuGetPackageSource(NuGetPackage package, FileName fileName)
        {
            NuGetPackage = package;
            FileName = fileName;
        }
    }

    internal struct NuGetPackageConflict
    {
        internal NuGetPackageSource Original { get; }
        internal NuGetPackageSource Conflict { get; }
        internal string PackageName => Original.NuGetPackage.Name;

        internal NuGetPackageConflict(NuGetPackageSource original, NuGetPackageSource conflict)
        {
            Debug.Assert(Constants.NugetPackageNameComparer.Equals(original.NuGetPackage.Name, conflict.NuGetPackage.Name));
            Original = original;
            Conflict = conflict;
        }
    }
}
