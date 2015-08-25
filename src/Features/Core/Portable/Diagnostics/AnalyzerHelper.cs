// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerHelper
    {
        private const string CSharpCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer";
        private const string VisualBasicCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer";

        private const string AnalyzerExceptionDiagnosticId = "AD0001";
        private const string AnalyzerExceptionDiagnosticCategory = "Intellisense";
        

        public static bool IsBuiltInAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            return analyzer is IBuiltInAnalyzer || analyzer is DocumentDiagnosticAnalyzer || analyzer is ProjectDiagnosticAnalyzer || analyzer.IsCompilerAnalyzer();
        }

        public static bool IsCompilerAnalyzer(this DiagnosticAnalyzer analyzer)
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

        public static ValueTuple<string, VersionStamp> GetAnalyzerIdAndVersion(this DiagnosticAnalyzer analyzer)
        {
            // Get the unique ID for given diagnostic analyzer.
            // note that we also put version stamp so that we can detect changed analyzer.
            var type = analyzer.GetType();
            var typeInfo = type.GetTypeInfo();
            return ValueTuple.Create(GetAssemblyQualifiedName(type), GetAnalyzerVersion(CorLightup.Desktop.GetAssemblyLocation(typeInfo.Assembly)));
        }

        public static string GetAnalyzerAssemblyName(this DiagnosticAnalyzer analyzer)
        {
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return typeInfo.Assembly.GetName().Name;
        }

        private static string GetAssemblyQualifiedName(Type type)
        {
            // AnalyzerFileReference now includes things like versions, public key as part of its identity. 
            // so we need to consider them.
            return type.AssemblyQualifiedName;
        }

        internal static void OnAnalyzerException_NoTelemetryLogging(
            Exception ex,
            DiagnosticAnalyzer analyzer,
            Diagnostic diagnostic,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            ProjectId projectIdOpt)
        {
            if (diagnostic != null)
            {
                hostDiagnosticUpdateSource?.ReportAnalyzerDiagnostic(analyzer, diagnostic, hostDiagnosticUpdateSource?.Workspace, projectIdOpt);
            }

            if (IsBuiltInAnalyzer(analyzer))
            {
                FatalError.ReportWithoutCrashUnlessCanceled(ex);
            }
        }

        internal static void OnAnalyzerExceptionForSupportedDiagnostics(DiagnosticAnalyzer analyzer, Exception exception, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            if (exception is OperationCanceledException)
            {
                return;
            }

            var diagnostic = CreateAnalyzerExceptionDiagnostic(analyzer, exception);
            OnAnalyzerException_NoTelemetryLogging(exception, analyzer, diagnostic, hostDiagnosticUpdateSource, projectIdOpt: null);
        }

        /// <summary>
        /// Create a diagnostic for exception thrown by the given analyzer.
        /// </summary>
        /// <remarks>
        /// Keep this method in sync with "AnalyzerExecutor.CreateAnalyzerExceptionDiagnostic".
        /// </remarks>
        internal static Diagnostic CreateAnalyzerExceptionDiagnostic(DiagnosticAnalyzer analyzer, Exception e)
        {
            var analyzerName = analyzer.ToString();

            // TODO: It is not ideal to create a new descriptor per analyzer exception diagnostic instance.
            // However, until we add a LongMessage field to the Diagnostic, we are forced to park the instance specific description onto the Descriptor's Description field.
            // This requires us to create a new DiagnosticDescriptor instance per diagnostic instance.
            var descriptor = new DiagnosticDescriptor(AnalyzerExceptionDiagnosticId,
                title: FeaturesResources.UserDiagnosticAnalyzerFailure,
                messageFormat: FeaturesResources.UserDiagnosticAnalyzerThrows,
                description: string.Format(FeaturesResources.UserDiagnosticAnalyzerThrowsDescription, analyzerName, e.ToString()),
                category: AnalyzerExceptionDiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);

            return Diagnostic.Create(descriptor, Location.None, analyzerName, e.GetType(), e.Message);
        }

        private static VersionStamp GetAnalyzerVersion(string path)
        {
            if (path == null || !PortableShim.File.Exists(path))
            {
                return VersionStamp.Default;
            }

            return VersionStamp.Create(PortableShim.File.GetLastWriteTimeUtc(path));
        }
    }
}