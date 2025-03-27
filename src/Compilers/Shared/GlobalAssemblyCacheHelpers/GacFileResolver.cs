// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Resolves assembly identities in Global Assembly Cache.
    /// </summary>
    internal sealed class GacFileResolver : IEquatable<GacFileResolver>
    {
        /// <summary>
        /// Returns true if GAC is available on the current platform.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                // Since mscorlib may not be loaded from the GAC on Mono, also check if the platform is Mono which supports a GAC.
                return
#if !NETCOREAPP
                typeof(object).Assembly.GlobalAssemblyCache ||
#endif
                PlatformInformation.IsRunningOnMono;
            }
        }

        /// <summary>
        /// Architecture filter used when resolving assembly references.
        /// </summary>
        public ImmutableArray<ProcessorArchitecture> Architectures { get; }

        /// <summary>
        /// <see cref="CultureInfo"/> used when resolving assembly references, or null to prefer no culture.
        /// </summary>
        public CultureInfo PreferredCulture { get; }

        /// <summary>
        /// Creates an instance of a <see cref="GacFileResolver"/>, if available on the platform (check <see cref="IsAvailable"/>).
        /// </summary>
        /// <param name="architectures">Supported architectures used to filter GAC assemblies.</param>
        /// <param name="preferredCulture">A culture to use when choosing the best assembly from
        /// among the set filtered by <paramref name="architectures"/></param>
        /// <exception cref="PlatformNotSupportedException">The platform doesn't support GAC.</exception>
        public GacFileResolver(
            ImmutableArray<ProcessorArchitecture> architectures = default,
            CultureInfo preferredCulture = null)
        {
            if (!IsAvailable)
            {
                throw new PlatformNotSupportedException();
            }

            if (architectures.IsDefault)
            {
                architectures = GlobalAssemblyCache.CurrentArchitectures;
            }

            Architectures = architectures;
            PreferredCulture = preferredCulture;
        }

        public string Resolve(string assemblyName)
        {
            string path;
            GlobalAssemblyCache.Instance.ResolvePartialName(assemblyName, out path, Architectures, this.PreferredCulture);
            return File.Exists(path) ? path : null;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(PreferredCulture, Hash.CombineValues(Architectures));
        }

        public bool Equals(GacFileResolver other)
        {
            return ReferenceEquals(this, other) ||
                other != null &&
                Architectures.SequenceEqual(other.Architectures) &&
                PreferredCulture == other.PreferredCulture;
        }

        public override bool Equals(object obj) => Equals(obj as GacFileResolver);
    }
}
