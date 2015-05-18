// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerHelper
    {
        private const string CSharpCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer";
        private const string VisualBasicCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer";

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
            return ValueTuple.Create(GetAssemblyQualifiedNameWithoutVersion(type), GetAnalyzerVersion(type.Assembly.Location));
        }

        public static string GetAnalyzerAssemblyName(this DiagnosticAnalyzer analyzer)
        {
            var type = analyzer.GetType();
            return type.Assembly.GetName().Name;
        }

        private static string GetAssemblyQualifiedNameWithoutVersion(Type type)
        {
            var name = type.AssemblyQualifiedName;
            var versionIndex = name.IndexOf(", Version=", StringComparison.InvariantCultureIgnoreCase);
            if (versionIndex < 0)
            {
                return name;
            }

            return name.Substring(0, versionIndex);
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

            return AnalyzerExecutor.CreateForSupportedDiagnostics(onAnalyzerException ?? defaultOnAnalyzerException, AnalyzerManager.Instance, cancellationToken: cancellationToken);
        }

        internal static void OnAnalyzerException_NoTelemetryLogging(
            Exception e,
            DiagnosticAnalyzer analyzer,
            Diagnostic diagnostic,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            ProjectId projectIdOpt = null)
        {
            if (diagnostic != null)
            {
                hostDiagnosticUpdateSource?.ReportAnalyzerDiagnostic(analyzer, diagnostic, hostDiagnosticUpdateSource?.Workspace, projectIdOpt);
            }

            if (IsBuiltInAnalyzer(analyzer))
            {
                FatalError.ReportWithoutCrashUnlessCanceled(e);
            }
        }

        private static VersionStamp GetAnalyzerVersion(string path)
        {
            if (path == null || !File.Exists(path))
            {
                return VersionStamp.Default;
            }

            return VersionStamp.Create(File.GetLastWriteTimeUtc(path));
        }

        public static ReportDiagnostic GetEffectiveSeverity(
            this Compilation compilation,
            string ruleId,
            DiagnosticSeverity defaultSeverity,
            bool enabledByDefault)
        {
            return GetEffectiveSeverity(
                compilation.Options,
                ruleId,
                defaultSeverity,
                enabledByDefault);
        }

        public static ReportDiagnostic GetEffectiveSeverity(
            this CompilationOptions options,
            string ruleId,
            DiagnosticSeverity defaultSeverity,
            bool enabledByDefault)
        {
            return GetEffectiveSeverity(
                options?.GeneralDiagnosticOption ?? ReportDiagnostic.Default,
                options?.SpecificDiagnosticOptions,
                ruleId,
                defaultSeverity,
                enabledByDefault);
        }

        public static ReportDiagnostic GetEffectiveSeverity(
            this DiagnosticDescriptor descriptor,
            CompilationOptions options)
        {
            return GetEffectiveSeverity(
                options,
                descriptor.Id,
                descriptor.DefaultSeverity,
                descriptor.IsEnabledByDefault);
        }

        public static ReportDiagnostic GetEffectiveSeverity(
            ReportDiagnostic generalOption,
            IDictionary<string, ReportDiagnostic> specificOptions,
            string ruleId,
            DiagnosticSeverity defaultSeverity,
            bool enabledByDefault)
        {
            ReportDiagnostic report = ReportDiagnostic.Default;
            var isSpecified = specificOptions?.TryGetValue(ruleId, out report) ?? false;
            if (!isSpecified)
            {
                report = enabledByDefault ? ReportDiagnostic.Default : ReportDiagnostic.Suppress;
            }

            if (report == ReportDiagnostic.Default)
            {
                switch (generalOption)
                {
                    case ReportDiagnostic.Error:
                        if (defaultSeverity == DiagnosticSeverity.Warning)
                        {
                            if (!isSpecified)
                            {
                                return ReportDiagnostic.Error;
                            }
                        }

                        break;
                    case ReportDiagnostic.Suppress:
                        if (defaultSeverity == DiagnosticSeverity.Warning || defaultSeverity == DiagnosticSeverity.Info)
                        {
                            return ReportDiagnostic.Suppress;
                        }

                        break;
                    default:
                        break;
                }

                return MapSeverityToReport(defaultSeverity);
            }

            return report;
        }

        private static ReportDiagnostic MapSeverityToReport(DiagnosticSeverity defaultSeverity)
        {
            switch (defaultSeverity)
            {
                case DiagnosticSeverity.Hidden:
                    return ReportDiagnostic.Hidden;
                case DiagnosticSeverity.Info:
                    return ReportDiagnostic.Info;
                case DiagnosticSeverity.Warning:
                    return ReportDiagnostic.Warn;
                case DiagnosticSeverity.Error:
                    return ReportDiagnostic.Error;
                default:
                    throw new ArgumentException("Unhandled DiagnosticSeverity: " + defaultSeverity, "defaultSeverity");
            }
        }
    }
}