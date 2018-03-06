﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Manages properties of analyzers (such as registered actions, supported diagnostics) for analyzer host's lifetime
    /// and executes the callbacks into the analyzers.
    /// 
    /// It ensures the following for the lifetime of analyzer host:
    /// 1) <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> is invoked only once per-analyzer.
    /// 2) <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is invoked only once per-analyzer.
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-compilation per-analyzer and analyzer options.
    /// </summary>
    internal partial class AnalyzerManager
    {
        // This cache stores the analyzer execution context per-analyzer (i.e. registered actions, supported descriptors, etc.).
        private readonly ImmutableDictionary<DiagnosticAnalyzer, AnalyzerExecutionContext> _analyzerExecutionContextMap;

        public AnalyzerManager(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            _analyzerExecutionContextMap = CreateAnalyzerExecutionContextMap(analyzers);
        }

        public AnalyzerManager(DiagnosticAnalyzer analyzer)
        {
            _analyzerExecutionContextMap = CreateAnalyzerExecutionContextMap(SpecializedCollections.SingletonEnumerable(analyzer));
        }

        private ImmutableDictionary<DiagnosticAnalyzer, AnalyzerExecutionContext> CreateAnalyzerExecutionContextMap(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerExecutionContext>();
            foreach (var analyzer in analyzers)
            {
                builder.Add(analyzer, new AnalyzerExecutionContext());
            }

            return builder.ToImmutable();
        }

        private AnalyzerExecutionContext GetAnalyzerExecutionContext(DiagnosticAnalyzer analyzer) => _analyzerExecutionContextMap[analyzer];

        private async Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetCompilationAnalysisScopeCoreAsync(sessionScope, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
        }

        private async Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeCoreAsync(
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext)
        {
            try
            {
                return await analyzerExecutionContext.GetCompilationAnalysisScopeAsync(sessionScope, analyzerExecutor).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the compilation scope for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearCompilationScopeTask();

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetCompilationAnalysisScopeCoreAsync(sessionScope, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
            }
        }

        private async Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetSessionAnalysisScopeCoreAsync(analyzer, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
        }

        private async Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeCoreAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext)
        {
            try
            {
                var task = analyzerExecutionContext.GetSessionAnalysisScopeTask(analyzer, analyzerExecutor);
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearSessionScopeTask();

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetSessionAnalysisScopeCoreAsync(analyzer, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the analyzer actions to execute for the given analyzer against a given compilation.
        /// The returned actions include the actions registered during <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> method as well as
        /// the actions registered during <see cref="CompilationStartAnalyzerAction"/> for the given compilation.
        /// </summary>
        public async Task<AnalyzerActions> GetAnalyzerActionsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            if (sessionScope.CompilationStartActions.Length > 0 && analyzerExecutor.Compilation != null)
            {
                var compilationScope = await GetCompilationAnalysisScopeAsync(analyzer, sessionScope, analyzerExecutor).ConfigureAwait(false);
                return compilationScope.GetAnalyzerActions(analyzer);
            }

            return sessionScope.GetAnalyzerActions(analyzer);
        }

        /// <summary>
        /// Returns true if the given analyzer has enabled concurrent execution by invoking <see cref="AnalysisContext.EnableConcurrentExecution"/>.
        /// </summary>
        public async Task<bool> IsConcurrentAnalyzerAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            return sessionScope.IsConcurrentAnalyzer(analyzer);
        }

        /// <summary>
        /// Returns <see cref="GeneratedCodeAnalysisFlags"/> for the given analyzer.
        /// If an analyzer hasn't configured generated code analysis, returns <see cref="AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags"/>.
        /// </summary>
        public async Task<GeneratedCodeAnalysisFlags> GetGeneratedCodeAnalysisFlagsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            return sessionScope.GetGeneratedCodeAnalysisFlags(analyzer);
        }

        private static void ForceLocalizableStringExceptions(LocalizableString localizableString, EventHandler<Exception> handler)
        {
            if (localizableString.CanThrowExceptions)
            {
                localizableString.OnException += handler;
                localizableString.ToString();
                localizableString.OnException -= handler;
            }
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return analyzerExecutionContext.GetOrComputeDescriptors(analyzer, analyzerExecutor);
        }

        internal bool IsSupportedDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer, AnalyzerExecutor analyzerExecutor)
        {
            // Avoid realizing all the descriptors for all compiler diagnostics by assuming that compiler analyzer doesn't report unsupported diagnostics.
            if (isCompilerAnalyzer(analyzer))
            {
                return true;
            }

            if (IsCompilerReservedDiagnostic(diagnostic.Id))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.CompilerDiagnosticIdReported, diagnostic.Id), nameof(diagnostic));
            }

            // Get all the supported diagnostics and scan them linearly to see if the reported diagnostic is supported by the analyzer.
            // The linear scan is okay, given that this runs only if a diagnostic is being reported and a given analyzer is quite unlikely to have hundreds of thousands of supported diagnostics.
            var supportedDescriptors = GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor);
            foreach (var descriptor in supportedDescriptors)
            {
                if (descriptor.Id.Equals(diagnostic.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCompilerReservedDiagnostic(string id)
        {
            // Only the compiler analyzer should produce diagnostics with CS or BC prefixes (followed by digit)
            if (id.Length >= 3 && (id.StartsWith("CS", StringComparison.Ordinal) || id.StartsWith("BC", StringComparison.Ordinal)))
            {
                char thirdChar = id[2];
                return thirdChar >= '0' && thirdChar <= '9';
            }

            return false;
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            if (isCompilerAnalyzer(analyzer))
            {
                // Compiler analyzer must always be executed for compiler errors, which cannot be suppressed or filtered.
                return false;
            }

            var supportedDiagnostics = GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor);
            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
                if (HasNotConfigurableTag(diag.CustomTags))
                {
                    if (diag.IsEnabledByDefault)
                    {
                        // Diagnostic descriptor is not configurable, so the diagnostics created through it cannot be suppressed.
                        return false;
                    }
                    else
                    {
                        // NotConfigurable disabled diagnostic can be ignored as it is never reported.
                        continue;
                    }
                }

                // Is this diagnostic suppressed by default (as written by the rule author)
                var isSuppressed = !diag.IsEnabledByDefault;

                // If the user said something about it, that overrides the author.
                if (diagnosticOptions.TryGetValue(diag.Id, out var severity))
                {
                    isSuppressed = severity == ReportDiagnostic.Suppress;
                }

                if (!isSuppressed)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool HasNotConfigurableTag(IEnumerable<string> customTags)
        {
            foreach (var customTag in customTags)
            {
                if (customTag == WellKnownDiagnosticTags.NotConfigurable)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
