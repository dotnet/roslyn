// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerHelper
    {
        private const string CSharpCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer";
        private const string VisualBasicCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer";

        // These are the error codes of the compiler warnings. 
        // Keep the ids the same so that de-duplication against compiler errors
        // works in the error list (after a build).
        internal const string WRN_AnalyzerCannotBeCreatedIdCS = "CS8032";
        internal const string WRN_AnalyzerCannotBeCreatedIdVB = "BC42376";
        internal const string WRN_NoAnalyzerInAssemblyIdCS = "CS8033";
        internal const string WRN_NoAnalyzerInAssemblyIdVB = "BC42377";
        internal const string WRN_UnableToLoadAnalyzerIdCS = "CS8034";
        internal const string WRN_UnableToLoadAnalyzerIdVB = "BC42378";

        // Shared with Compiler
        internal const string AnalyzerExceptionDiagnosticId = "AD0001";
        internal const string AnalyzerDriverExceptionDiagnosticId = "AD0002";

        // IDE only errors
        internal const string WRN_AnalyzerCannotBeCreatedId = "AD1000";
        internal const string WRN_NoAnalyzerInAssemblyId = "AD1001";
        internal const string WRN_UnableToLoadAnalyzerId = "AD1002";

        private const string AnalyzerExceptionDiagnosticCategory = "Intellisense";

        public static bool IsWorkspaceDiagnosticAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            return analyzer is DocumentDiagnosticAnalyzer || analyzer is ProjectDiagnosticAnalyzer;
        }

        public static bool IsBuiltInAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            return analyzer is IBuiltInAnalyzer || analyzer.IsWorkspaceDiagnosticAnalyzer() || analyzer.IsCompilerAnalyzer();
        }

        public static bool IsOpenFileOnly(this DiagnosticAnalyzer analyzer, Workspace workspace)
        {
            if (analyzer is IBuiltInAnalyzer builtInAnalyzer)
            {
                return builtInAnalyzer.OpenFileOnly(workspace);
            }

            return false;
        }

        public static bool ContainsOpenFileOnlyAnalyzers(this CompilationWithAnalyzers analyzerDriverOpt, Workspace workspace)
        {
            if (analyzerDriverOpt == null)
            {
                // not Roslyn. no open file only analyzers
                return false;
            }

            foreach (var analyzer in analyzerDriverOpt.Analyzers)
            {
                if (analyzer.IsOpenFileOnly(workspace))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasNonHiddenDescriptor(this DiagnosticAnalyzerService service, DiagnosticAnalyzer analyzer, Project project)
        {
            // most of analyzers, number of descriptor is quite small, so this should be cheap.
            return service.GetDiagnosticDescriptors(analyzer).Any(d => d.GetEffectiveSeverity(project.CompilationOptions) != ReportDiagnostic.Hidden);
        }

        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions options)
        {
            return options == null
                ? descriptor.DefaultSeverity.ToReportDiagnostic()
                : descriptor.GetEffectiveSeverity(options);
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

        public static (string analyzerId, VersionStamp version) GetAnalyzerIdAndVersion(this DiagnosticAnalyzer analyzer)
        {
            // Get the unique ID for given diagnostic analyzer.
            // note that we also put version stamp so that we can detect changed analyzer.
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return (analyzer.GetAnalyzerId(), GetAnalyzerVersion(CorLightup.Desktop.GetAssemblyLocation(typeInfo.Assembly)));
        }

        public static string GetAnalyzerAssemblyName(this DiagnosticAnalyzer analyzer)
        {
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return typeInfo.Assembly.GetName().Name;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public static ValueTask<OptionSet> GetDocumentOptionSetAsync(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var workspaceAnalyzerOptions = analyzerOptions as WorkspaceAnalyzerOptions;
            if (workspaceAnalyzerOptions == null)
            {
                return new ValueTask<OptionSet>(default(OptionSet));
            }

            return workspaceAnalyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken);
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
                title: FeaturesResources.User_Diagnostic_Analyzer_Failure,
                messageFormat: FeaturesResources.Analyzer_0_threw_an_exception_of_type_1_with_message_2,
                description: string.Format(FeaturesResources.Analyzer_0_threw_the_following_exception_colon_1, analyzerName, e.CreateDiagnosticDescription()),
                category: AnalyzerExceptionDiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);

            return Diagnostic.Create(descriptor, Location.None, analyzerName, e.GetType(), e.Message);
        }

        private static VersionStamp GetAnalyzerVersion(string path)
        {
            if (path == null || !File.Exists(path))
            {
                return VersionStamp.Default;
            }

            return VersionStamp.Create(File.GetLastWriteTimeUtc(path));
        }

        public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(string fullPath, AnalyzerLoadFailureEventArgs e)
        {
            return CreateAnalyzerLoadFailureDiagnostic(null, null, null, fullPath, e);
        }

        public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(
            Workspace workspace, ProjectId projectId, string language, string fullPath, AnalyzerLoadFailureEventArgs e)
        {
            if (!TryGetErrorMessage(language, fullPath, e, out var id, out var message, out var messageFormat, out var description))
            {
                return null;
            }

            return new DiagnosticData(
                id,
                FeaturesResources.Roslyn_HostError,
                message,
                messageFormat,
                severity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: description,
                warningLevel: 0,
                workspace: workspace,
                projectId: projectId);
        }

        private static bool TryGetErrorMessage(
            string language, string fullPath, AnalyzerLoadFailureEventArgs e,
            out string id, out string message, out string messageFormat, out string description)
        {
            switch (e.ErrorCode)
            {
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer:
                    id = Choose(language, WRN_UnableToLoadAnalyzerId, WRN_UnableToLoadAnalyzerIdCS, WRN_UnableToLoadAnalyzerIdVB);
                    messageFormat = FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1;
                    message = string.Format(FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1, fullPath, e.Message);
                    description = e.Exception.CreateDiagnosticDescription();
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer:
                    id = Choose(language, WRN_AnalyzerCannotBeCreatedId, WRN_AnalyzerCannotBeCreatedIdCS, WRN_AnalyzerCannotBeCreatedIdVB);
                    messageFormat = FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2;
                    message = string.Format(FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2, e.TypeName, fullPath, e.Message);
                    description = e.Exception.CreateDiagnosticDescription();
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers:
                    id = Choose(language, WRN_NoAnalyzerInAssemblyId, WRN_NoAnalyzerInAssemblyIdCS, WRN_NoAnalyzerInAssemblyIdVB);
                    messageFormat = FeaturesResources.The_assembly_0_does_not_contain_any_analyzers;
                    message = string.Format(FeaturesResources.The_assembly_0_does_not_contain_any_analyzers, fullPath);
                    description = e.Exception.CreateDiagnosticDescription();
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.None:
                default:
                    id = string.Empty;
                    message = string.Empty;
                    messageFormat = string.Empty;
                    description = string.Empty;
                    return false;
            }

            return true;
        }

        private static string Choose(string language, string noLanguageMessage, string csharpMessage, string vbMessage)
        {
            if (language == null)
            {
                return noLanguageMessage;
            }

            return language == LanguageNames.CSharp ? csharpMessage : vbMessage;
        }

        public static void AppendAnalyzerMap(this Dictionary<string, DiagnosticAnalyzer> analyzerMap, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                // user might have included exact same analyzer twice as project analyzers explicitly. we consider them as one
                analyzerMap[analyzer.GetAnalyzerId()] = analyzer;
            }
        }

        public static IEnumerable<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(this IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult, IDiagnosticAnalyzerService serviceOpt = null)
        {
            return Convert(analysisResult.Select(kv => (kv.Key, kv.Value.ExecutionTime)), serviceOpt);
        }

        private static IEnumerable<AnalyzerPerformanceInfo> Convert(IEnumerable<(DiagnosticAnalyzer analyzer, TimeSpan timeSpan)> analyzerPerf, IDiagnosticAnalyzerService serviceOpt = null)
        {
            return analyzerPerf.Select(kv => new AnalyzerPerformanceInfo(kv.analyzer.GetAnalyzerId(), DiagnosticAnalyzerLogger.AllowsTelemetry(kv.analyzer, serviceOpt), kv.timeSpan));
        }
    }
}
