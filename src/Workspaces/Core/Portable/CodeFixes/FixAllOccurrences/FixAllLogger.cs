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
        private static readonly string CodeFixProvider = "CodeFixProvider";
        private static readonly string CodeActionId = "CodeActionId";
        private static readonly string FixAllScope = "FixAllScope";
        private static readonly string LanguageName = "LanguageName";
        private static readonly string DocumentCount = "DocumentCount";

        // Fix all computation result logging.
        private static readonly string Result = "Result";
        private static readonly string Completed = "Completed";
        private static readonly string TimedOut = "TimedOut";
        private static readonly string Cancelled = "Cancelled";
        private static readonly string AllChangesApplied = "AllChangesApplied";
        private static readonly string SubsetOfChangesApplied = "SubsetOfChangesApplied";

        // Diagnostics and fixes logging.
        private static readonly string DocumentsWithDiagnosticsToFix = "DocumentsWithDiagnosticsToFix";
        private static readonly string ProjectsWithDiagnosticsToFix = "ProjectsWithDiagnosticsToFix";
        private static readonly string TotalDiagnosticsToFix = "TotalDiagnosticsToFix";
        private static readonly string TotalFixesToMerge = "TotalFixesToMerge";

        public static void LogContext(FixAllContext fixAllContext, bool isInternalCodeFixProvider)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesContext, KeyValueLogMessage.Create(m =>
            {
                if (isInternalCodeFixProvider)
                {
                    m[CodeFixProvider] = fixAllContext.CodeFixProvider.GetType().FullName;
                    m[CodeActionId] = fixAllContext.CodeActionId;
                    m[LanguageName] = fixAllContext.Project.Language;
                }
                else
                {
                    m[CodeFixProvider] = fixAllContext.CodeFixProvider.GetType().FullName.GetHashCode().ToString();
                    m[CodeActionId] = fixAllContext.CodeActionId != null ? fixAllContext.CodeActionId.GetHashCode().ToString() : null;
                    m[LanguageName] = fixAllContext.Project.Language.GetHashCode().ToString();
                }
                
                m[FixAllScope] = fixAllContext.Scope.ToString();
                switch (fixAllContext.Scope)
                {
                    case CodeFixes.FixAllScope.Project:
                        m[DocumentCount] = fixAllContext.Project.DocumentIds.Count.ToString();
                        break;

                    case CodeFixes.FixAllScope.Solution:
                        m[DocumentCount] = fixAllContext.Solution.Projects.Sum(p => p.DocumentIds.Count).ToString();
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
                value = Completed;
            }
            else if (timedOut)
            {
                value = TimedOut;
            }
            else
            {
                value = Cancelled;
            }

            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation, KeyValueLogMessage.Create(m =>
            {
                m[Result] = value;
            }));
        }

        public static void LogPreviewChangesResult(bool applied, bool allChangesApplied = true)
        {
            string value;
            if (applied)
            {
                value = allChangesApplied ?
                    AllChangesApplied :
                    SubsetOfChangesApplied;
            }
            else
            {
                value = Cancelled;
            }

            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesPreviewChanges, KeyValueLogMessage.Create(m =>
            {
                m[Result] = value;
            }));
        }

        public static void LogDiagnosticsStats(ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, KeyValueLogMessage.Create(m =>
            {
                m[DocumentsWithDiagnosticsToFix] = documentsAndDiagnosticsToFixMap.Keys.Count().ToString();
                m[TotalDiagnosticsToFix] = documentsAndDiagnosticsToFixMap.Values.Sum(v => v.Length).ToString();
            }));
        }

        public static void LogDiagnosticsStats(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, KeyValueLogMessage.Create(m =>
            {
                m[ProjectsWithDiagnosticsToFix] = projectsAndDiagnosticsToFixMap.Keys.Count().ToString();
                m[TotalDiagnosticsToFix] = projectsAndDiagnosticsToFixMap.Values.Sum(v => v.Length).ToString();
            }));
        }

        public static void LogFixesToMergeStats(ConcurrentBag<CodeAction> fixesToMerge)
        {
            Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Merge, KeyValueLogMessage.Create(m =>
            {
                m[TotalFixesToMerge] = fixesToMerge.Count.ToString();
            }));
        }
    }
}