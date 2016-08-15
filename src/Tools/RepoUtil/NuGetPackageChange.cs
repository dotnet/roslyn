// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal sealed class NuGetPackageChange
    {
        internal string Name { get; }
        internal string OldVersion { get; }
        internal string NewVersion { get; }
        internal NuGetPackage OldPackage => new NuGetPackage(Name, OldVersion);
        internal NuGetPackage NewPackage => new NuGetPackage(Name, NewVersion);

        internal NuGetPackageChange(string name, string oldVersion, string newVersion)
        {
            Name = name;
            OldVersion = oldVersion;
            NewVersion = newVersion;
        }

        public override string ToString() => $"{Name} from {OldVersion} to {NewVersion}";
    }
}
