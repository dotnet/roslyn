// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;

namespace Metalama.Compiler
{
    internal class ComponentInfo : IComponentInfo
    {
        private readonly ISourceTransformer _transformer;
        private readonly AssemblyMetadataReader _metadataReader;

        public ComponentInfo(ISourceTransformer transformer)
        {
            _transformer = transformer;
            this._metadataReader = AssemblyMetadataReader.GetInstance(transformer.GetType().Assembly);
        }

        public string? Company => this._metadataReader.Company;
        public string Name => _transformer.GetType().FullName!;
        public string Version => this._metadataReader.PackageVersion;
        public bool IsPrerelease => this.Version.Contains("-");
        public DateTime BuildDate => this._metadataReader.BuildDate;
    }

    /// <summary>
    /// Provide application information stored using <see cref="AssemblyMetadataAttribute"/>.
    /// </summary>
    internal class MetalamaCompilerApplicationInfo : ApplicationInfoBase
    {
        private readonly bool _ignoreUnattendedProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetalamaCompilerApplicationInfo"/> class.
        /// </summary>
        /// <exception cref="InvalidOperationException">Some of the required assembly metadata were not found.</exception>
        public MetalamaCompilerApplicationInfo(bool isLongRunningProcess, bool ignoreUnattendedProcess, ImmutableArray<ISourceTransformer> components) : base(typeof(MetalamaCompilerApplicationInfo).Assembly) 
        {
            _ignoreUnattendedProcess = ignoreUnattendedProcess;
            this.IsLongRunningProcess = isLongRunningProcess;

            this.Components = components.Select(x=>new ComponentInfo(x)).ToImmutableArray<IComponentInfo>();
        }

        /// <inheritdoc />
        public override ProcessKind ProcessKind => ProcessKind.Compiler;

        /// <inheritdoc />
        public override bool IsUnattendedProcess(ILoggerFactory loggerFactory) => !_ignoreUnattendedProcess && ProcessUtilities.IsCurrentProcessUnattended(loggerFactory);

        /// <inheritdoc />
        public override bool IsLongRunningProcess { get; }

        /// <inheritdoc />
        public override string Name => "Metalama.Compiler";

        /// <inheritdoc />
        public override bool IsTelemetryEnabled { get; }

        public override ImmutableArray<IComponentInfo> Components { get; }
    }
}
