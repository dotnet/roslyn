// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;

namespace Metalama.Compiler
{
    internal class ComponentInfo : ComponentInfoBase
    {
        public ComponentInfo(ISourceTransformer transformer) : base(transformer.GetType().Assembly)
        {
            var transformerType = transformer.GetType();
            this.Name = transformerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ??
                        transformerType.FullName!;
        }

        public override string Name { get; }
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
        public MetalamaCompilerApplicationInfo(bool isLongRunningProcess, bool ignoreUnattendedProcess, ImmutableArray<ISourceTransformer> transformers) : base(typeof(MetalamaCompilerApplicationInfo).Assembly)
        {
            _ignoreUnattendedProcess = ignoreUnattendedProcess;
            this.IsLongRunningProcess = isLongRunningProcess;

            this.Components = transformers.Select(x => new ComponentInfo(x)).ToImmutableArray<IComponentInfo>();
        }

        /// <inheritdoc />
        public override ProcessKind ProcessKind => ProcessKind.Compiler;

        /// <inheritdoc />
        public override bool IsUnattendedProcess(ILoggerFactory loggerFactory) => !_ignoreUnattendedProcess && base.IsUnattendedProcess(loggerFactory);

        /// <inheritdoc />
        public override bool IsLongRunningProcess { get; }

        /// <inheritdoc />
        public override string Name => "Metalama.Compiler";

        public override ImmutableArray<IComponentInfo> Components { get; }
    }
}
