// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerManager
    {
        private sealed class AnalyzerExecutionContext
        {
            private readonly object _gate = new object();

            /// <summary>
            /// Task to compute HostSessionStartAnalysisScope for session wide analyzer actions, i.e. AnalyzerActions registered by analyzer's Initialize method.
            /// These are run only once per every analyzer. 
            /// </summary>
            private Task<HostSessionStartAnalysisScope> _lazySessionScopeTask;

            /// <summary>
            /// Task to compute HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
            /// </summary>
            private Task<HostCompilationStartAnalysisScope> _lazyCompilationScopeTask;

            /// <summary>
            /// Supported descriptors for diagnostic analyzer.
            /// </summary>
            private ImmutableArray<DiagnosticDescriptor> _lazyDescriptors = default(ImmutableArray<DiagnosticDescriptor>);

            public Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeTask(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
            {
                lock (_gate)
                {
                    Task<HostSessionStartAnalysisScope> task;
                    if (_lazySessionScopeTask != null)
                    {
                        return _lazySessionScopeTask;
                    }

                    task = Task.Run(() =>
                    {
                        var sessionScope = new HostSessionStartAnalysisScope();
                        analyzerExecutor.ExecuteInitializeMethod(analyzer, sessionScope);
                        return sessionScope;
                    }, analyzerExecutor.CancellationToken);

                    _lazySessionScopeTask = task;
                    return task;
                }
            }

            public void ClearSessionScopeTask()
            {
                lock (_gate)
                {
                    _lazySessionScopeTask = null;
                }
            }

            public Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
                HostSessionStartAnalysisScope sessionScope,
                AnalyzerExecutor analyzerExecutor)
            {
                lock (_gate)
                {
                    if (_lazyCompilationScopeTask == null)
                    {
                        _lazyCompilationScopeTask = Task.Run(() =>
                        {
                            var compilationAnalysisScope = new HostCompilationStartAnalysisScope(sessionScope);
                            analyzerExecutor.ExecuteCompilationStartActions(sessionScope.CompilationStartActions, compilationAnalysisScope);
                            return compilationAnalysisScope;
                        }, analyzerExecutor.CancellationToken);
                    }

                    return _lazyCompilationScopeTask;
                }
            }

            public void ClearCompilationScopeTask()
            {
                lock (_gate)
                {
                    _lazyCompilationScopeTask = null;
                }
            }

            public ImmutableArray<DiagnosticDescriptor> GetOrComputeDescriptors(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
            {
                lock (_gate)
                {
                    if (!_lazyDescriptors.IsDefault)
                    {
                        return _lazyDescriptors;
                    }
                }

                // Otherwise, compute the value.
                // We do so outside the lock statement as we are calling into user code, which may be a long running operation.
                var descriptors = ComputeDescriptors(analyzer, analyzerExecutor);

                lock (_gate)
                {
                    // Check if another thread already stored the computed value.
                    if (!_lazyDescriptors.IsDefault)
                    {
                        // If so, we return the stored value.
                        descriptors = _lazyDescriptors;
                    }
                    else
                    {
                        // Otherwise, store the value computed here.
                        _lazyDescriptors = descriptors;
                    }
                }

                return descriptors;
            }

            /// <summary>
            /// Compute <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> and exception handler for the given <paramref name="analyzer"/>.
            /// </summary>
            private static ImmutableArray<DiagnosticDescriptor> ComputeDescriptors(
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor)
            {
                var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

                // Catch Exception from analyzer.SupportedDiagnostics
                analyzerExecutor.ExecuteAndCatchIfThrows(
                    analyzer,
                    _ =>
                    {
                        var supportedDiagnosticsLocal = analyzer.SupportedDiagnostics;
                        if (!supportedDiagnosticsLocal.IsDefaultOrEmpty)
                        {
                            supportedDiagnostics = supportedDiagnosticsLocal;
                        }
                    },
                    argument: default(object));

                // Force evaluate and report exception diagnostics from LocalizableString.ToString().
                Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = analyzerExecutor.OnAnalyzerException;
                if (onAnalyzerException != null)
                {
                    var handler = new EventHandler<Exception>((sender, ex) =>
                    {
                        var diagnostic = AnalyzerExecutor.CreateAnalyzerExceptionDiagnostic(analyzer, ex);
                        onAnalyzerException(ex, analyzer, diagnostic);
                    });

                    foreach (var descriptor in supportedDiagnostics)
                    {
                        ForceLocalizableStringExceptions(descriptor.Title, handler);
                        ForceLocalizableStringExceptions(descriptor.MessageFormat, handler);
                        ForceLocalizableStringExceptions(descriptor.Description, handler);
                    }
                }

                return supportedDiagnostics;
            }
        }
    }
}
