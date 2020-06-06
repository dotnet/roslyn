// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Globalization;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Settings that affect InteractiveHost process and initialization.
    /// </summary>
    internal sealed class InteractiveHostOptions
    {
        /// <summary>
        /// Optional path to the .rsp file to process when initializing context of the process.
        /// </summary>
        public string? InitializationFile { get; }

        /// <summary>
        /// Host culture used for localization of doc comments, errors.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Path to interactive host directory (this directory is expected to contain Core and Desktop subdirectories).
        /// </summary>
        public string HostDirectory { get; }

        /// <summary>
        /// Host process platform.
        /// </summary>
        public InteractiveHostPlatform Platform { get; }

        public InteractiveHostOptions(
            string hostDirectory,
            string? initializationFile = null,
            CultureInfo? culture = null,
            InteractiveHostPlatform platform = InteractiveHostPlatform.Desktop32)
        {
            Contract.ThrowIfNull(hostDirectory);
            HostDirectory = hostDirectory;
            InitializationFile = initializationFile;
            Culture = culture ?? CultureInfo.CurrentUICulture;
            Platform = platform;
        }

        public string GetHostPath()
            => Path.Combine(
                HostDirectory,
                (Platform == InteractiveHostPlatform.Core) ? "Core" : "Desktop",
                "InteractiveHost" + (Platform == InteractiveHostPlatform.Desktop32 ? "32" : "64") + ".exe");
    }
}
