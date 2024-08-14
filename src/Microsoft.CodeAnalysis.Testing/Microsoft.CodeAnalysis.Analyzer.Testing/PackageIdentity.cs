// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using NuGet.Versioning;

#if !NETCOREAPP
using System.Collections.Generic;
#endif

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Represents the core identity of a NuGet package.
    /// </summary>
    /// <seealso cref="NuGet.Packaging.Core.PackageIdentity"/>
    public sealed class PackageIdentity : IEquatable<PackageIdentity?>
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

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(Id, Version);
#else
            var hashCode = -612338121;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Version);
            return hashCode;
#endif
        }

        public override bool Equals(object? obj)
            => Equals(obj as PackageIdentity);

        public bool Equals(PackageIdentity? other)
        {
            return other is not null
                && Id == other.Id
                && Version == other.Version;
        }

        internal NuGet.Packaging.Core.PackageIdentity ToNuGetIdentity()
        {
            return new NuGet.Packaging.Core.PackageIdentity(Id, NuGetVersion.Parse(Version));
        }
    }
}
