// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// DiagnosticAnalyzer for compiler's syntax/semantic/compilation diagnostics.
    /// </summary>
    internal abstract partial class CompilerDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

        protected abstract CommonMessageProvider MessageProvider { get; }

        // internal as this is called from tests
        internal abstract ImmutableArray<int> GetSupportedErrorCodes();

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => InterlockedOperations.Initialize(
                ref _supportedDiagnostics,
                createArray: static @this =>
                {
                    var messageProvider = @this.MessageProvider;
                    var errorCodes = @this.GetSupportedErrorCodes();
                    var builder = ArrayBuilder<DiagnosticDescriptor>.GetInstance(errorCodes.Length);
                    foreach (var errorCode in errorCodes)
                        builder.Add(DiagnosticInfo.GetDescriptor(errorCode, messageProvider));

                    builder.Add(AnalyzerExecutor.GetAnalyzerExceptionDiagnosticDescriptor());
                    return builder.ToImmutableAndFree();
                }, arg: this);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(c =>
            {
                var analyzer = new CompilationAnalyzer(c.Compilation);
                c.RegisterSyntaxTreeAction(analyzer.AnalyzeSyntaxTree);
                c.RegisterSemanticModelAction(CompilationAnalyzer.AnalyzeSemanticModel);
            });
        }
    }
}
