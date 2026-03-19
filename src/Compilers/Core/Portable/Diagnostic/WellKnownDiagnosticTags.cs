// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public static class WellKnownDiagnosticTags
    {
        /// <summary>
        /// Indicates that the diagnostic is related to some unnecessary source code.
        /// </summary>
        public const string Unnecessary = nameof(Unnecessary);

        /// <summary>
        /// Indicates that the diagnostic is related to edit and continue.
        /// </summary>
        public const string EditAndContinue = nameof(EditAndContinue);

        /// <summary>
        /// Indicates that the diagnostic is related to build.
        /// </summary>
        /// <remarks>
        /// Build errors are recognized to potentially represent stale results from a point in the past when the computation occurred.
        /// An example of when Roslyn produces non-live errors is with an explicit user gesture to "run code analysis".
        /// Because these represent errors from the past, we do want them to be superseded by a more recent live run,
        /// or a more recent build from another source.
        /// </remarks>
        public const string Build = nameof(Build);

        /// <summary>
        /// Indicates that the diagnostic is reported by the compiler.
        /// </summary>
        public const string Compiler = nameof(Compiler);

        /// <summary>
        /// Indicates that the diagnostic can be used for telemetry
        /// </summary>
        public const string Telemetry = nameof(Telemetry);

        /// <summary>
        /// Indicates that the diagnostic is not configurable, i.e. it cannot be suppressed or filtered or have its severity changed.
        /// </summary>
        public const string NotConfigurable = nameof(NotConfigurable);

        /// <summary>
        /// Indicates that the analyzer reporting the diagnostic supports custom severity configuration mechanism(s)
        /// to allow end users to configure effective severity of the diagnostic.
        /// Such analyzers are always considered to be enabled by the compiler and always receive analyzer callbacks.
        /// Additionally, severity of the diagnostics reported with this custom tag is not altered by analyzer config options
        /// to configure severity, i.e. 'dotnet_diagnostic' and 'dotnet_analyzer_diagnostic' entries. 
        /// </summary>
        /// <remarks>
        /// See https://github.com/dotnet/roslyn/issues/52991 for further details.
        /// </remarks>
        public const string CustomSeverityConfigurable = nameof(CustomSeverityConfigurable);

        /// <summary>
        /// Indicates that the diagnostic is related to an exception thrown by a <see cref="DiagnosticAnalyzer"/>.
        /// </summary>
        public const string AnalyzerException = nameof(AnalyzerException);

        /// <summary>
        /// Indicates that the diagnostic is an obsolete diagnostic with a custom ID
        /// specified by the 'DiagnosticId' property on 'ObsoleteAttribute'.
        /// </summary>
        public const string CustomObsolete = nameof(CustomObsolete);

        /// <summary>
        /// Indicates that the diagnostic is a compilation end diagnostic reported
        /// from a compilation end action.
        /// </summary>
        public const string CompilationEnd = nameof(CompilationEnd);
    }
}
