// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerHelper
    {
        private const string CSharpCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer";
        private const string VisualBasicCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer";

        public static bool IsBuiltInAnalyzer(DiagnosticAnalyzer analyzer)
        {
            return analyzer is IBuiltInAnalyzer || analyzer is DocumentDiagnosticAnalyzer || analyzer is ProjectDiagnosticAnalyzer || IsCompilerAnalyzer(analyzer);
        }

        public static bool IsCompilerAnalyzer(DiagnosticAnalyzer analyzer)
        {
            // TODO: find better way.
            var typeString = analyzer.GetType().ToString();
            if (typeString == CSharpCompilerAnalyzerTypeName)
            {
                return true;
            }

            if (typeString == VisualBasicCompilerAnalyzerTypeName)
            {
                return true;
            }

            return false;
        }

        public static Action<Diagnostic> GetAddExceptionDiagnosticDelegate(DiagnosticAnalyzer analyzer, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource, Project project)
        {
            return diagnostic =>
                hostDiagnosticUpdateSource?.ReportAnalyzerDiagnostic(analyzer, diagnostic, project.Solution.Workspace, project);
        }

        public static Action<Diagnostic> GetAddExceptionDiagnosticDelegate(DiagnosticAnalyzer analyzer, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource, Workspace workspace)
        {
            return diagnostic =>
                hostDiagnosticUpdateSource?.ReportAnalyzerDiagnostic(analyzer, diagnostic, workspace, null);
        }

        public static AnalyzerExecutor GetAnalyzerExecutorForSupportedDiagnostics(
            DiagnosticAnalyzer analyzer,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var addExceptionDiagnostic = GetAddExceptionDiagnosticDelegate(analyzer, hostDiagnosticUpdateSource, hostDiagnosticUpdateSource?.Workspace);

            // Skip telemetry logging if the exception is thrown as we are computing supported diagnostics and
            // we can't determine if any descriptors support getting telemetry without having the descriptors.
            return AnalyzerExecutor.CreateForSupportedDiagnostics(addExceptionDiagnostic, continueOnAnalyzerException, cancellationToken);
        }

        public static AnalyzerExecutor GetAnalyzerExecutor(
            DiagnosticAnalyzer analyzer,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            Project project,
            Compilation compilation,
            Action<Diagnostic> addDiagnostic,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var addExceptionDiagnostic = GetAddExceptionDiagnosticDelegate(analyzer, hostDiagnosticUpdateSource, project);
            return AnalyzerExecutor.Create(compilation, analyzerOptions, addDiagnostic, addExceptionDiagnostic, continueOnAnalyzerException, cancellationToken);
        }
    }
}