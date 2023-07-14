// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
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

        /// <summary>
        /// Supported suppressions of each <see cref="DiagnosticSuppressor"/>. 
        /// </summary>
        /// <remarks>
        /// Holds on <see cref="DiagnosticSuppressor"/> instances weakly so that we don't keep suppressors coming from package references alive.
        /// They need to be released when the project stops referencing the suppressor.
        /// 
        /// The purpose of this map is to avoid multiple calls to <see cref="DiagnosticSuppressor.SupportedSuppressions"/> that might return different values
        /// (they should not but we need a guarantee to function correctly).
        /// </remarks>
        private readonly ConditionalWeakTable<DiagnosticSuppressor, SuppressionDescriptorsInfo> _suppressionsInfo;

        /// <summary>
        /// Lazily populated map from diagnostic IDs to diagnostic descriptor.
        /// If same diagnostic ID is reported by multiple descriptors, a null value is stored in the map for that ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, DiagnosticDescriptor?> _idToDescriptorsMap;

        private sealed class DiagnosticDescriptorsInfo(ImmutableArray<DiagnosticDescriptor> supportedDescriptors, bool telemetryAllowed)
        {
            public readonly ImmutableArray<DiagnosticDescriptor> SupportedDescriptors = supportedDescriptors;
            public readonly bool TelemetryAllowed = telemetryAllowed;
            public readonly bool HasCompilationEndDescriptor = supportedDescriptors.Any(DiagnosticDescriptorExtensions.IsCompilationEnd);
        }

        private sealed class SuppressionDescriptorsInfo(ImmutableArray<SuppressionDescriptor> supportedSuppressions)
        {
            public readonly ImmutableArray<SuppressionDescriptor> SupportedSuppressions = supportedSuppressions;
        }

        [Export, Shared]
        internal sealed class SharedGlobalCache
        {
            public readonly DiagnosticAnalyzerInfoCache AnalyzerInfoCache = new();

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public SharedGlobalCache()
            {
            }
        }

        internal DiagnosticAnalyzerInfoCache()
        {
            _descriptorsInfo = new ConditionalWeakTable<DiagnosticAnalyzer, DiagnosticDescriptorsInfo>();
            _suppressionsInfo = new ConditionalWeakTable<DiagnosticSuppressor, SuppressionDescriptorsInfo>();
            _idToDescriptorsMap = new ConcurrentDictionary<string, DiagnosticDescriptor?>();
        }

        /// <summary>
        /// Returns <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).SupportedDescriptors;

        /// <summary>
        /// Returns <see cref="DiagnosticSuppressor.SupportedSuppressions"/> of given <paramref name="suppressor"/>.
        /// </summary>
        public ImmutableArray<SuppressionDescriptor> GetDiagnosticSuppressions(DiagnosticSuppressor suppressor)
            => GetOrCreateSuppressionsInfo(suppressor).SupportedSuppressions;

        /// <summary>
        /// Returns <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>
        /// that are not compilation end descriptors.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetNonCompilationEndDiagnosticDescriptors(DiagnosticAnalyzer analyzer)
        {
            var descriptorInfo = GetOrCreateDescriptorsInfo(analyzer);
            return !descriptorInfo.HasCompilationEndDescriptor
                ? descriptorInfo.SupportedDescriptors
                : descriptorInfo.SupportedDescriptors.WhereAsArray(d => !d.IsCompilationEnd());
        }

        /// <summary>
        /// Returns true if given <paramref name="analyzer"/> has a compilation end descriptor
        /// that is reported in the Compilation end action.
        /// </summary>
        public bool IsCompilationEndAnalyzer(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).HasCompilationEndDescriptor;

        /// <summary>
        /// Determine whether collection of telemetry is allowed for given <paramref name="analyzer"/>.
        /// </summary>
        public bool IsTelemetryCollectionAllowed(DiagnosticAnalyzer analyzer)
            => GetOrCreateDescriptorsInfo(analyzer).TelemetryAllowed;

        public bool TryGetDescriptorForDiagnosticId(string diagnosticId, [NotNullWhen(true)] out DiagnosticDescriptor? descriptor)
            => _idToDescriptorsMap.TryGetValue(diagnosticId, out descriptor) && descriptor != null;

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

            PopulateIdToDescriptorMap(descriptors);
            var telemetryAllowed = IsTelemetryCollectionAllowed(analyzer, descriptors);
            return new DiagnosticDescriptorsInfo(descriptors, telemetryAllowed);
        }

        private SuppressionDescriptorsInfo GetOrCreateSuppressionsInfo(DiagnosticSuppressor suppressor)
            => _suppressionsInfo.GetValue(suppressor, CalculateSuppressionsInfo);

        private SuppressionDescriptorsInfo CalculateSuppressionsInfo(DiagnosticSuppressor suppressor)
        {
            ImmutableArray<SuppressionDescriptor> suppressions;
            try
            {
                // SupportedSuppressions is user code and can throw an exception.
                suppressions = suppressor.SupportedSuppressions.NullToEmpty();
            }
            catch
            {
                // No need to report the exception to the user.
                // Eventually, when the suppressor runs the compiler analyzer driver will report a diagnostic.
                suppressions = ImmutableArray<SuppressionDescriptor>.Empty;
            }

            return new SuppressionDescriptorsInfo(suppressions);
        }

        private static bool IsTelemetryCollectionAllowed(DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticDescriptor> descriptors)
            => analyzer.IsCompilerAnalyzer() ||
               analyzer is IBuiltInAnalyzer ||
               descriptors.Length > 0 && descriptors[0].ImmutableCustomTags().Any(static t => t == WellKnownDiagnosticTags.Telemetry);

        private void PopulateIdToDescriptorMap(ImmutableArray<DiagnosticDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                if (!_idToDescriptorsMap.TryGetValue(descriptor.Id, out var existingDescriptor))
                {
                    _idToDescriptorsMap[descriptor.Id] = descriptor;
                }
                else if (existingDescriptor != null && !descriptor.Equals(existingDescriptor))
                {
                    // Multiple descriptors with same diagnostic ID, store null in the map.
                    // Exception case: Many CAxxxx analyzers use multiple descriptors with same ID which differ only in MessageFormat.
                    //                 This allows analyzer to report slightly differing diagnostic messages with same ID.
                    //                 We handle this case here by allowing existing descriptor to be used.
                    if (descriptor.WithMessageFormat(existingDescriptor.MessageFormat).Equals(existingDescriptor))
                        continue;

                    _idToDescriptorsMap[descriptor.Id] = null;
                }
            }
        }
    }
}
