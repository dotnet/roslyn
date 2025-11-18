// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace BuildBoss
{
    internal readonly struct PackageReference
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
