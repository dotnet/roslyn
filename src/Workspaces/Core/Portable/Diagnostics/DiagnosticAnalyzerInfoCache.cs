// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provides and caches information about diagnostic analyzers such as <see cref="AnalyzerReference"/>, 
    /// <see cref="DiagnosticAnalyzer"/> instance, <see cref="DiagnosticDescriptor"/>s.
    /// Thread-safe.
    /// </summary>
    internal sealed partial class DiagnosticAnalyzerInfoCache
    {
        /// <summary>
        /// Supported descriptors of each <see cref="DiagnosticAnalyzer"/>. 
        /// </summary>
        /// <remarks>
        /// Holds on <see cref="DiagnosticAnalyzer"/> instances weakly so that we don't keep analyzers coming from package references alive.
        /// They need to be released when the project stops referencing the analyzer.
        /// 
        /// The purpose of this map is to avoid multiple calls to <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> that might return different values
        /// (they should not but we need a guarantee to function correctly).
        /// </remarks>
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticDescriptorsInfo> _descriptorsInfo;

        private sealed class DiagnosticDescriptorsInfo
        {
            public readonly ImmutableArray<DiagnosticDescriptor> SupportedDescriptors;
            public readonly bool TelemetryAllowed;

            public DiagnosticDescriptorsInfo(ImmutableArray<DiagnosticDescriptor> supportedDescriptors, bool telemetryAllowed)
            {
                SupportedDescriptors = supportedDescriptors;
                TelemetryAllowed = telemetryAllowed;
            }
        }

        internal DiagnosticAnalyzerInfoCache()
        {
            _descriptorsInfo = new ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticDescriptorsInfo>();
        }

        /// <summary>
        /// Returns <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).SupportedDescriptors;

        /// <summary>
        /// Determine whether collection of telemetry is allowed for given <paramref name="analyzer"/>.
        /// </summary>
        public bool IsTelemetryCollectionAllowed(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).TelemetryAllowed;

        private DiagnosticDescriptorsInfo GetOrCreateDescriptorsInfo(DiagnosticAnalyzer analyzer)
            => _descriptorsInfo.GetValue(analyzer, CalculateDescriptorsInfo);

        private DiagnosticDescriptorsInfo CalculateDescriptorsInfo(DiagnosticAnalyzer analyzer)
        {
            ImmutableArray<DiagnosticDescriptor> descriptors;
            try
            {
                // SupportedDiagnostics is user code and can throw an exception.
                descriptors = analyzer.SupportedDiagnostics.NullToEmpty();
            }
            catch
            {
                // No need to report the exception to the user.
                // Eventually, when the analyzer runs the compiler analyzer driver will report a diagnostic.
                descriptors = ImmutableArray<DiagnosticDescriptor>.Empty;
            }

            var telemetryAllowed = IsTelemetryCollectionAllowed(analyzer, descriptors);
            return new DiagnosticDescriptorsInfo(descriptors, telemetryAllowed);
        }

        private static bool IsTelemetryCollectionAllowed(DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticDescriptor> descriptors)
            => analyzer.IsCompilerAnalyzer() ||
               analyzer is IBuiltInAnalyzer ||
               descriptors.Length > 0 && descriptors[0].CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry);

        /// <summary>
        /// Return true if the given <paramref name="analyzer"/> is suppressed for the given project.
        /// NOTE: This API is intended to be used only for performance optimization.
        /// </summary>
        public bool IsAnalyzerSuppressed(DiagnosticAnalyzer analyzer, Project project)
        {
            var options = project.CompilationOptions;
            if (options == null || analyzer == FileContentLoadAnalyzer.Instance || analyzer.IsCompilerAnalyzer())
            {
                return false;
            }

            // If user has disabled analyzer execution for this project, we only want to execute required analyzers
            // that report diagnostics with category "Compiler".
            if (!project.State.RunAnalyzers &&
                GetDiagnosticDescriptors(analyzer).All(d => d.Category != DiagnosticCategory.Compiler))
            {
                return true;
            }

            // NOTE: Previously we used to return "CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(options)"
            //       on this code path, which returns true if analyzer is suppressed through compilation options.
            //       However, this check is no longer correct as analyzers can be enabled/disabled for individual
            //       documents through .editorconfig files. So we pessimistically assume analyzer is not suppressed
            //       and let the core analyzer driver in the compiler layer handle skipping redundant analysis callbacks.
            return false;
        }
    }
}
