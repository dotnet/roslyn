// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NuGet.Versioning;

namespace Microsoft.CodeAnalysis.Testing
{
    public sealed class PackageIdentity
    {
        public PackageIdentity(string id, string version)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public string Id { get; }

        public string Version { get; }

        internal NuGet.Packaging.Core.PackageIdentity ToNuGetIdentity()
        {
            return new NuGet.Packaging.Core.PackageIdentity(Id, NuGetVersion.Parse(Version));
        }
    }
}
