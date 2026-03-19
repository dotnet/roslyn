// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Settings that affect InteractiveHost process and initialization.
    /// </summary>
    internal sealed class InteractiveHostOptions
    {
        /// <summary>
        /// Path to interactive host executable.
        /// </summary>
        public string HostPath { get; }

        /// <summary>
        /// Optional file name of the .rsp file to use to initialize the REPL.
        /// </summary>
        public string? InitializationFilePath { get; }

        /// <summary>
        /// Host culture used for data formatting.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Host culture used for localization of doc comments, errors.
        /// </summary>
        public CultureInfo UICulture { get; }

        /// <summary>
        /// Host process platform.
        /// </summary>
        public InteractiveHostPlatform Platform { get; }

        public InteractiveHostOptions(
            string hostPath,
            string? initializationFilePath,
            CultureInfo culture,
            CultureInfo uiCulture,
            InteractiveHostPlatform platform)
        {
            Contract.ThrowIfNull(hostPath);

            HostPath = hostPath;
            InitializationFilePath = initializationFilePath;
            Culture = culture;
            UICulture = uiCulture;
            Platform = platform;
        }

        public static InteractiveHostOptions CreateFromDirectory(
            string hostDirectory,
            string? initializationFileName,
            CultureInfo culture,
            CultureInfo uiCulture,
            InteractiveHostPlatform platform)
        {
            var hostSubdirectory = (platform == InteractiveHostPlatform.Core) ? "Core" : "Desktop";
            var hostExecutableFileName = "InteractiveHost" + (platform == InteractiveHostPlatform.Desktop32 ? "32" : "64") + ".exe";

            var hostPath = Path.Combine(hostDirectory, hostSubdirectory, hostExecutableFileName);
            var initializationFilePath = (initializationFileName != null) ? Path.Combine(hostDirectory, hostSubdirectory, initializationFileName) : null;

            return new InteractiveHostOptions(hostPath, initializationFilePath, culture, uiCulture, platform);
        }
    }
}
