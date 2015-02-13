// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Manages analyzers for analyzer host's lifetime.
    /// 
    /// It ensures the following for the lifetime of analyzer host:
    /// 1) <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> is invoked only once per-analyzer.
    /// 2) <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is invoked only once per-analyzer.
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-analyzer per-compilation.
    /// </summary>
    internal class AnalyzerManager
    {
        public static readonly AnalyzerManager Default = new AnalyzerManager();

        // Session wide analyzer actions map that stores HostSessionStartAnalysisScope registered by running the Initialize method on every DiagnosticAnalyzer.
        // These are run only once per every analyzer.
        private ImmutableDictionary<DiagnosticAnalyzer, HostSessionStartAnalysisScope> _sessionScopeMap =
            ImmutableDictionary<DiagnosticAnalyzer, HostSessionStartAnalysisScope>.Empty;

        // This map stores the per-compilation HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
        // Compilation start actions will get executed once per-each compilation as user might want to return different set of custom actions for each compilation.
        private readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<DiagnosticAnalyzer, HostCompilationStartAnalysisScope>> _compilationScopeMap =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<DiagnosticAnalyzer, HostCompilationStartAnalysisScope>>();

        /// <summary>
        /// Cache descriptors for each diagnostic analyzer. We do this since <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is
        /// a property rather than metadata. We expect it to be cheap and immutable, but we can't force them to be so, we cache them
        /// and ask only once.
        /// </summary>
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyList<DiagnosticDescriptor>> _descriptorCache =
            new ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyList<DiagnosticDescriptor>>();

        internal HostCompilationStartAnalysisScope GetCompilationAnalysisScope(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            Compilation compilation,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var sessionActions = sessionScope.GetAnalyzerActions(analyzer);
            Debug.Assert(sessionActions.CompilationStartActionsCount > 0);

            var compilationActionsMap = _compilationScopeMap.GetOrCreateValue(compilation);
            HostCompilationStartAnalysisScope result;
            if (compilationActionsMap.TryGetValue(analyzer, out result))
            {
                return result;
            }

            result = new HostCompilationStartAnalysisScope(sessionScope);
            AnalyzerDriverHelper.ExecuteCompilationStartActions(sessionActions.CompilationStartActions, result, compilation,
                analyzerOptions, addDiagnostic, continueOnAnalyzerException, cancellationToken);

            if (!compilationActionsMap.TryAdd(analyzer, result))
            {
                return compilationActionsMap[analyzer];
            }

            return result;
        }

        internal HostSessionStartAnalysisScope GetSessionAnalysisScope(
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            return ImmutableInterlocked.GetOrAdd(ref _sessionScopeMap, analyzer, _ =>
            {
                var sessionScope = new HostSessionStartAnalysisScope();
                AnalyzerDriverHelper.ExecuteInitializeMethod(analyzer, sessionScope, addDiagnostic, continueOnAnalyzerException, cancellationToken);
                return sessionScope;
            });
        }

        /// <summary>
        /// Get all the analyzer actions to execute for the given analyzer against a given compilation.
        /// </summary>
        public AnalyzerActions GetAnalyzerActions(
            DiagnosticAnalyzer analyzer,
            Compilation compilation,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var sessionScope = GetSessionAnalysisScope(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken);
            var sessionActions = sessionScope.GetAnalyzerActions(analyzer);
            if (sessionActions != null && sessionActions.CompilationStartActionsCount > 0 && compilation != null)
            {
                var compilationScope = GetCompilationAnalysisScope(analyzer, sessionScope, 
                    compilation, addDiagnostic, analyzerOptions, continueOnAnalyzerException, cancellationToken);
                var compilationActions = compilationScope.GetAnalyzerActions(analyzer);

                if (compilationActions != null)
                {
                    return sessionActions.Append(compilationActions);
                }
            }

            return sessionActions;
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var descriptors = _descriptorCache.GetValue(analyzer, key =>
            {
                var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

                // Catch Exception from analyzer.SupportedDiagnostics
                AnalyzerDriverHelper.ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () => { supportedDiagnostics = analyzer.SupportedDiagnostics; }, cancellationToken);

                return supportedDiagnostics;
            });

            return (ImmutableArray<DiagnosticDescriptor>)descriptors;
        }
    }
}
