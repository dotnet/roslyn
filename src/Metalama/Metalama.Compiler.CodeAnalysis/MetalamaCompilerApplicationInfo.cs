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
            var metadataAttributes =
                typeof(MetalamaCompilerApplicationInfo).Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute),
                    inherit: false);

            string? version = null;
            bool? isPrerelease = null;
            DateTime? buildDate = null;

            bool AllMetadataFound() => version != null && isPrerelease != null && buildDate != null;

            foreach (var metadataAttributeObject in metadataAttributes)
            {
                var metadataAttribute = (AssemblyMetadataAttribute)metadataAttributeObject;

                switch (metadataAttribute.Key)
                {
                    case "MetalamaCompilerVersion":
                        if (!string.IsNullOrEmpty(metadataAttribute.Value))
                        {
                            version = metadataAttribute.Value;
                            isPrerelease = version.Contains('-');
                        }

                        break;

                    case "MetalamaCompilerBuildDate":
                        if (!string.IsNullOrEmpty(metadataAttribute.Value))
                        {
                            buildDate = DateTime.Parse(metadataAttribute.Value, CultureInfo.InvariantCulture);
                        }

                        break;
                }

                if (AllMetadataFound())
                {
                    break;
                }
            }

            if (!AllMetadataFound())
            {
                throw new InvalidOperationException(
                    $"{nameof(MetalamaCompilerApplicationInfo)} has failed to find some of the required assembly metadata.");
            }

            this.Version = version!;
            this.IsPrerelease = isPrerelease!.Value;
            this.BuildDate = buildDate!.Value;
            this.IsLongRunningProcess = isLongRunningProcess;
        }

        /// <inheritdoc />
        public DateTime BuildDate { get; }

        public ProcessKind ProcessKind => ProcessKind.Compiler;
        public bool IsUnattendedProcess(ILoggerFactory loggerFactory) => !_ignoreUnattendedProcess && ProcessUtilities.IsCurrentProcessUnattended(loggerFactory);
        public bool IsLongRunningProcess { get; }

        /// <inheritdoc />
        public string Name => "Metalama Compiler";

        /// <inheritdoc />
        public string Version { get; }

        /// <inheritdoc />
        public bool IsPrerelease { get; }
    }
}
