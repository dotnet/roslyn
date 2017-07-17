// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Experimentation
{
    internal static class AnalyzerABTestLogger
    {
        private static bool s_reportErrors = false;
        private static readonly ConcurrentDictionary<(object, string), object> s_reported = new ConcurrentDictionary<(object, string), object>(concurrencyLevel: 2, capacity: 10);

        private const string Name = "LiveCodeAnalysisVsix";

        public static void Log(string action)
        {
            Logger.Log(FunctionId.Experiment_ABTesting, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[nameof(Name)] = Name;
                m[nameof(action)] = action;
            }));
        }

        public static void LogInstallationStatus(Workspace workspace, LiveCodeAnalysisInstallStatus installStatus)
        {
            var vsixInstalled = workspace.Options.GetOption(AnalyzerABTestOptions.VsixInstalled);
            if (!vsixInstalled && installStatus == LiveCodeAnalysisInstallStatus.Installed)
            {
                // first time after vsix installed
                workspace.Options = workspace.Options.WithChangedOption(AnalyzerABTestOptions.VsixInstalled, true);
                workspace.Options = workspace.Options.WithChangedOption(AnalyzerABTestOptions.ParticipatedInExperiment, true);
                Log("Installed");

                // set the system to report the errors.
                s_reportErrors = true;
            }

            if (vsixInstalled && installStatus == LiveCodeAnalysisInstallStatus.NotInstalled)
            {
                // first time after vsix is uninstalled
                workspace.Options = workspace.Options.WithChangedOption(AnalyzerABTestOptions.VsixInstalled, false);
                Log("Uninstalled");
            }
        }

        public static void LogCandidacyRequirementsTracking(long lastTriggeredTimeBinary)
        {
            if (lastTriggeredTimeBinary == AnalyzerABTestOptions.LastDateTimeUsedSuggestionAction.DefaultValue)
            {
                Log("StartCandidacyRequirementsTracking");
            }
        }

        public static void LogProjectDiagnostics(Project project, string analyzerName, DiagnosticAnalysisResult result)
        {
            if (!s_reportErrors || !s_reported.TryAdd((project.Id, analyzerName), null))
            {
                // doesn't meet the bar to report the issue.
                return;
            }

            // logs count of errors for this project. this won't log anything if FSA off since
            // we don't collect any diagnostics for a project if FSA is off.
            var map = new Dictionary<string, int>();
            foreach (var documentId in result.DocumentIdsOrEmpty)
            {
                CountErrors(map, result.GetResultOrEmpty(result.SyntaxLocals, documentId));
                CountErrors(map, result.GetResultOrEmpty(result.SemanticLocals, documentId));
                CountErrors(map, result.GetResultOrEmpty(result.NonLocals, documentId));
            }

            CountErrors(map, result.Others);

            LogErrors(project, "ProjectDignostics", project.Id.Id, map);
        }

        public static void LogDocumentDiagnostics(Document document, string analyzerName, ImmutableArray<DiagnosticData> syntax, ImmutableArray<DiagnosticData> semantic)
        {
            if (!s_reportErrors || !s_reported.TryAdd((document.Id, analyzerName), null))
            {
                // doesn't meet the bar to report the issue.
                return;
            }

            // logs count of errors for this document. this only logs errors for 
            // this particular document. we do this since when FSA is off, this is
            // only errors we get. otherwise, we don't get any info when FSA is off and
            // that is default for C#.
            var map = new Dictionary<string, int>();
            CountErrors(map, syntax);
            CountErrors(map, semantic);

            LogErrors(document.Project, "DocumentDignostics", document.Id.Id, map);
        }

        private static void LogErrors(Project project, string action, Guid target, Dictionary<string, int> map)
        {
            if (map.Count == 0)
            {
                // nothing to report
                return;
            }

            var fsa = ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(project);
            Logger.Log(FunctionId.Experiment_ABTesting, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[nameof(Name)] = Name;
                m[nameof(action)] = action;
                m[nameof(target)] = target.ToString();
                m["FSA"] = fsa;
                m["errors"] = string.Join("|", map.Select(kv => $"{kv.Key}={kv.Value}"));
            }));
        }

        private static void CountErrors(Dictionary<string, int> map, ImmutableArray<DiagnosticData> diagnostics)
        {
            if (diagnostics.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var group in diagnostics.GroupBy(d => d.Id))
            {
                map[group.Key] = IDictionaryExtensions.GetValueOrDefault(map, group.Key) + group.Count();
            }
        }
    }
}
