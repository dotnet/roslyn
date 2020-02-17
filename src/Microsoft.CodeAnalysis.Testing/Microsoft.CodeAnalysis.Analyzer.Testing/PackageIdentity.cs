// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NuGet.Versioning;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Represents the core identity of a NuGet package.
    /// </summary>
    /// <seealso cref="NuGet.Packaging.Core.PackageIdentity"/>
    public sealed class PackageIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageIdentity"/> class with the specified name and version.
        /// </summary>
        /// <param name="id">The package name.</param>
        /// <param name="version">The package version.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>If <paramref name="id"/> is <see langword="null"/>.</para>
        /// <para>-or-</para>
        /// <para>If <paramref name="version"/> is <see langword="null"/>.</para>
        /// </exception>
        public PackageIdentity(string id, string version)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        /// <summary>
        /// Gets the package name.
        /// </summary>
        /// <seealso cref="NuGet.Packaging.Core.PackageIdentity.Id"/>
        public string Id { get; }

        /// <summary>
        /// Gets the package version.
        /// </summary>
        /// <seealso cref="NuGet.Packaging.Core.PackageIdentity.Version"/>
        public string Version { get; }

        internal NuGet.Packaging.Core.PackageIdentity ToNuGetIdentity()
        {
            return new NuGet.Packaging.Core.PackageIdentity(Id, NuGetVersion.Parse(Version));
        }
    }
}
