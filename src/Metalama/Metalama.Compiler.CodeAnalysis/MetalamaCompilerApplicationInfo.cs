// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;

namespace Metalama.Compiler
{
    /// <summary>
    /// Provide application information stored using <see cref="AssemblyMetadataAttribute"/>.
    /// </summary>
    internal class MetalamaCompilerApplicationInfo : IApplicationInfo
    {
        private readonly bool _ignoreUnattendedProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetalamaCompilerApplicationInfo"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Some of the required assembly metadata were not found.</exception>
        public MetalamaCompilerApplicationInfo(bool isLongRunningProcess, bool ignoreUnattendedProcess)
        {
            _ignoreUnattendedProcess = ignoreUnattendedProcess;

            var reader = AssemblyMetadataReader.GetInstance(typeof(MetalamaCompilerApplicationInfo).Assembly);

            if (!reader.TryGetValue("MetalamaCompilerVersion", out var version)
                || !reader.TryGetValue("MetalamaCompilerBuildDate", out var buildDate))
            {
                throw new InvalidOperationException(
                    $"{nameof(MetalamaCompilerApplicationInfo)} has failed to find some of the required assembly metadata.");
            }

            this.Version = version;
            this.BuildDate = DateTime.Parse(buildDate, CultureInfo.InvariantCulture);
            this.IsLongRunningProcess = isLongRunningProcess;

            // Parse the version set properties that depend on the kind of version.
            var versionParts = version.Split('-');

            if (versionParts.Length == 1)
            {
                this.IsPrerelease = false;
                this.IsTelemetryEnabled = true;
            }
            else
            {
                this.IsPrerelease = true;
                this.IsTelemetryEnabled = versionParts[1] is not ("dev" or "local");
            }
        }

        /// <inheritdoc />
        public DateTime BuildDate { get; }

        /// <inheritdoc />
        public ProcessKind ProcessKind => ProcessKind.Compiler;

        /// <inheritdoc />
        public bool IsUnattendedProcess(ILoggerFactory loggerFactory) => !_ignoreUnattendedProcess && ProcessUtilities.IsCurrentProcessUnattended(loggerFactory);

        /// <inheritdoc />
        public bool IsLongRunningProcess { get; }

        /// <inheritdoc />
        public string Name => "Metalama Compiler";

        /// <inheritdoc />
        public string Version { get; }

        /// <inheritdoc />
        public bool IsPrerelease { get; }

        /// <inheritdoc />
        public bool IsTelemetryEnabled { get; }
    }
}
