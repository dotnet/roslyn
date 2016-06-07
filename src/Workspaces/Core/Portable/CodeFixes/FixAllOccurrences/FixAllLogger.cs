// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Fix all occurrences logging.
    /// </summary>
    internal static class FixAllLogger
    {
        // Fix all context logging.
        private static readonly string s_codeFixProvider = "CodeFixProvider";
        private static readonly string s_codeActionEquivalenceKey = "CodeActionEquivalenceKey";
        private static readonly string s_fixAllScope = "FixAllScope";
        private static readonly string s_languageName = "LanguageName";
        private static readonly string s_documentCount = "DocumentCount";

        // Fix all computation result logging.
        private static readonly string s_result = "Result";
        private static readonly string s_completed = "Completed";
        private static readonly string s_timedOut = "TimedOut";
        private static readonly string s_cancelled = "Cancelled";
        private static readonly string s_allChangesApplied = "AllChangesApplied";
        private static readonly string s_subsetOfChangesApplied = "SubsetOfChangesApplied";

        // Diagnostics and fixes logging.
        private static readonly string s_documentsWithDiagnosticsToFix = "DocumentsWithDiagnosticsToFix";
        private static readonly string s_projectsWithDiagnosticsToFix = "ProjectsWithDiagnosticsToFix";
        private static readonly string s_totalDiagnosticsToFix = "TotalDiagnosticsToFix";
        private static readonly string s_totalFixesToMerge = "TotalFixesToMerge";

        public static void LogState(FixAllState fixAllState, bool isInternalCodeFixProvider)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesContext, KeyValueLogMessage.Create(m =>
            {
                if (isInternalCodeFixProvider)
                {
                    m[s_codeFixProvider] = fixAllState.CodeFixProvider.GetType().FullName;
                    m[s_codeActionEquivalenceKey] = fixAllState.CodeActionEquivalenceKey;
                    m[s_languageName] = fixAllState.Project.Language;
                }
                else
                {
                    m[s_codeFixProvider] = fixAllState.CodeFixProvider.GetType().FullName.GetHashCode().ToString();
                    m[s_codeActionEquivalenceKey] = fixAllState.CodeActionEquivalenceKey != null ? fixAllState.CodeActionEquivalenceKey.GetHashCode().ToString() : null;
                    m[s_languageName] = fixAllState.Project.Language.GetHashCode().ToString();
                }

                m[s_fixAllScope] = fixAllState.Scope.ToString();
                switch (fixAllState.Scope)
                {
                    case CodeFixes.FixAllScope.Project:
                        m[s_documentCount] = fixAllState.Project.DocumentIds.Count;
                        break;

                    case CodeFixes.FixAllScope.Solution:
                        m[s_documentCount] = fixAllState.Solution.Projects.Sum(p => p.DocumentIds.Count);
                        break;
                }
            }));
        }

        public static void LogComputationResult(bool completed, bool timedOut = false)
        {
            Contract.ThrowIfTrue(completed && timedOut);

            string value;
            if (completed)
            {
                value = s_completed;
            }
            else if (timedOut)
            {
                value = s_timedOut;
            }
            else
            {
                value = s_cancelled;
            }

            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation, KeyValueLogMessage.Create(m =>
            {
                m[s_result] = value;
            }));
        }

        public static void LogPreviewChangesResult(bool applied, bool allChangesApplied = true)
        {
            string value;
            if (applied)
            {
                value = allChangesApplied ? s_allChangesApplied : s_subsetOfChangesApplied;
            }
            else
            {
                value = s_cancelled;
            }

            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesPreviewChanges, KeyValueLogMessage.Create(m =>
            {
                m[s_result] = value;
            }));
        }

        public static void LogDiagnosticsStats(ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, KeyValueLogMessage.Create(m =>
            {
                m[s_documentsWithDiagnosticsToFix] = documentsAndDiagnosticsToFixMap.Keys.Count();
                m[s_totalDiagnosticsToFix] = documentsAndDiagnosticsToFixMap.Values.Sum(v => v.Length);
            }));
        }

        public static void LogDiagnosticsStats(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, KeyValueLogMessage.Create(m =>
            {
                m[s_projectsWithDiagnosticsToFix] = projectsAndDiagnosticsToFixMap.Keys.Count();
                m[s_totalDiagnosticsToFix] = projectsAndDiagnosticsToFixMap.Values.Sum(v => v.Length);
            }));
        }

        public static void LogFixesToMergeStats(ConcurrentBag<CodeAction> fixesToMerge)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, KeyValueLogMessage.Create(m =>
            {
                m[s_totalFixesToMerge] = fixesToMerge.Count;
            }));
        }
    }
}