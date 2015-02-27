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

        internal static AnalyzerExecutor GetAnalyzerExecutorForSupportedDiagnostics(
            DiagnosticAnalyzer analyzer,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Skip telemetry logging if the exception is thrown as we are computing supported diagnostics and
            // we can't determine if any descriptors support getting telemetry without having the descriptors.
            Action<Exception, DiagnosticAnalyzer, Diagnostic> defaultOnAnalyzerException = (ex, a, diagnostic) =>
                OnAnalyzerException_NoTelemetryLogging(ex, a, diagnostic, hostDiagnosticUpdateSource);

            return AnalyzerExecutor.CreateForSupportedDiagnostics(onAnalyzerException ?? defaultOnAnalyzerException, cancellationToken);
        }

        internal static void OnAnalyzerException_NoTelemetryLogging(
            Exception e,
            DiagnosticAnalyzer analyzer, 
            Diagnostic diagnostic,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            Project projectOpt = null)
        {
            if (diagnostic != null)
        {
                hostDiagnosticUpdateSource?.ReportAnalyzerDiagnostic(analyzer, diagnostic, hostDiagnosticUpdateSource?.Workspace, projectOpt);
        }
        
            if (IsBuiltInAnalyzer(analyzer))
        {
                FatalError.ReportWithoutCrashUnlessCanceled(e);
            }
        }
    }
}