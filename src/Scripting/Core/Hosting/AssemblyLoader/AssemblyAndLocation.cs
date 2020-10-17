﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal struct AssemblyAndLocation : IEquatable<AssemblyAndLocation>
    {
        public Assembly Assembly { get; }
        public string Location { get; }
        public bool GlobalAssemblyCache { get; }

        internal AssemblyAndLocation(Assembly assembly, string location, bool fromGac)
        {
            Debug.Assert(assembly != null && location != null);

            Assembly = assembly;
            Location = location;
            GlobalAssemblyCache = fromGac;
        }

        public bool IsDefault => Assembly == null;

        public bool Equals(AssemblyAndLocation other) =>
            Assembly == other.Assembly && Location == other.Location && GlobalAssemblyCache == other.GlobalAssemblyCache;

        public override int GetHashCode() =>
            Hash.Combine(Assembly, Hash.Combine(Location, Hash.Combine(GlobalAssemblyCache, 0)));

        public override bool Equals(object obj) =>
            obj is AssemblyAndLocation && Equals((AssemblyAndLocation)obj);

        public override string ToString() =>
            Assembly + " @ " + (GlobalAssemblyCache ? "<GAC>" : Location);
    }
}
