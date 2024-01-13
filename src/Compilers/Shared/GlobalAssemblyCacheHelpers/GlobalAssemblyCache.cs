// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    internal abstract class GlobalAssemblyCache
    {
        internal static readonly GlobalAssemblyCache Instance = CreateInstance();

        private static GlobalAssemblyCache CreateInstance()
        {
            if (Type.GetType("Mono.Runtime") != null)
            {
                return new MonoGlobalAssemblyCache();
            }
            else
            {
                return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET Framework")
                    ? new ClrGlobalAssemblyCache()
                    : new DotNetCoreGlobalAssemblyCache();
            }
        }

        /// <summary>
        /// Represents the current Processor architecture.
        /// </summary>
        public static readonly ImmutableArray<ProcessorArchitecture> CurrentArchitectures = (IntPtr.Size == 4)
            ? ImmutableArray.Create(ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.X86)
            : ImmutableArray.Create(ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.Amd64);

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">Optional partial name.</param>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        public abstract IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, ImmutableArray<ProcessorArchitecture> architectureFilter = default);

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">The optional partial name.</param>
        /// <param name="architectureFilter">The optional architecture filter.</param>
        public abstract IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string? partialName = null, ImmutableArray<ProcessorArchitecture> architectureFilter = default);

        /// <summary>
        /// Enumerates assemblies in the GAC returning their simple names.
        /// </summary>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        /// <returns>Unique simple names of GAC assemblies.</returns>
        public abstract IEnumerable<string> GetAssemblySimpleNames(ImmutableArray<ProcessorArchitecture> architectureFilter = default);

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching <see cref="AssemblyIdentity"/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly</param>
        /// <param name="architectureFilter">The optional processor architecture</param>
        /// <param name="preferredCulture">The optional preferred culture information</param>
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public AssemblyIdentity? ResolvePartialName(
            string displayName,
            ImmutableArray<ProcessorArchitecture> architectureFilter = default,
            CultureInfo? preferredCulture = null)
        {
            return ResolvePartialName(displayName, out _, architectureFilter, preferredCulture);
        }

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching <see cref="AssemblyIdentity"/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly</param>
        /// <param name="location">Full path name of the resolved assembly</param>
        /// <param name="architectureFilter">The optional processor architecture</param>
        /// <param name="preferredCulture">The optional preferred culture information</param>
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public abstract AssemblyIdentity? ResolvePartialName(
            string displayName,
            out string? location,
            ImmutableArray<ProcessorArchitecture> architectureFilter = default,
            CultureInfo? preferredCulture = null);
    }
}
