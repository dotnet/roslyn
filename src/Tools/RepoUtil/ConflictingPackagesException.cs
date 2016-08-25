using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RepoUtil
{
    internal class ConflictingPackagesException : Exception
    {
        public ConflictingPackagesException(List<NuGetPackageConflict> conflictingPackages) :
            base("Creation failed because of conflicting package versions.")
        {
            ConflictingPackages = conflictingPackages.ToImmutableArray();
        }

        public ImmutableArray<NuGetPackageConflict> ConflictingPackages { get; }
    }
}
