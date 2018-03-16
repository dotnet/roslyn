// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal struct PackageReference
    {
        internal string Name { get; }
        internal string Version { get; }

        internal PackageReference(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public override string ToString() => $"{Name} - {Version}";
    }
}
