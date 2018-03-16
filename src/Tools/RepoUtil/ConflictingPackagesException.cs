// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
