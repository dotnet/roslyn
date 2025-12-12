// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Fix all occurrences logging.
/// </summary>
internal static class FixAllLogger
{
    // correlation id of all events related to same instance of fix all
    public const string CorrelationId = nameof(CorrelationId);

    // Fix all context logging.
    private const string CodeFixProvider = nameof(CodeFixProvider);
    private const string CodeRefactoringProvider = nameof(CodeRefactoringProvider);
    private const string CodeActionEquivalenceKey = nameof(CodeActionEquivalenceKey);
    public const string FixAllScope = nameof(FixAllScope);
    private const string LanguageName = nameof(LanguageName);
    private const string DocumentCount = nameof(DocumentCount);

    // Fix all computation result logging.
    private const string Result = nameof(Result);
    private const string Completed = nameof(Completed);
    private const string TimedOut = nameof(TimedOut);
    private const string Cancelled = nameof(Cancelled);
    private const string AllChangesApplied = nameof(AllChangesApplied);
    private const string SubsetOfChangesApplied = nameof(SubsetOfChangesApplied);

    // Diagnostics and fixes logging.
    private const string DocumentsWithDiagnosticsToFix = nameof(DocumentsWithDiagnosticsToFix);
    private const string ProjectsWithDiagnosticsToFix = nameof(ProjectsWithDiagnosticsToFix);
    private const string TotalDiagnosticsToFix = nameof(TotalDiagnosticsToFix);
    private const string TotalFixesToMerge = nameof(TotalFixesToMerge);

    public static void LogState(IRefactorOrFixAllState fixAllState, bool isInternalProvider)
    {
        FunctionId functionId;
        string providerKey;
        switch (fixAllState.FixAllKind)
        {
            case FixAllKind.CodeFix:
                functionId = FunctionId.CodeFixes_FixAllOccurrencesContext;
                providerKey = CodeFixProvider;
                break;

            case FixAllKind.Refactoring:
                functionId = FunctionId.Refactoring_FixAllOccurrencesContext;
                providerKey = CodeRefactoringProvider;
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(fixAllState.FixAllKind);
        }

        Logger.Log(functionId, KeyValueLogMessage.Create(m =>
        {
            m[CorrelationId] = fixAllState.CorrelationId;

            if (isInternalProvider)
            {
                m[providerKey] = fixAllState.Provider.GetType().FullName!;
                m[CodeActionEquivalenceKey] = fixAllState.CodeActionEquivalenceKey;
                m[LanguageName] = fixAllState.Project.Language;
            }
            else
            {
                m[providerKey] = fixAllState.Provider.GetType().FullName!.GetHashCode().ToString();
                m[CodeActionEquivalenceKey] = fixAllState.CodeActionEquivalenceKey?.GetHashCode().ToString();
                m[LanguageName] = fixAllState.Project.Language.GetHashCode().ToString();
            }

            m[FixAllScope] = fixAllState.Scope.ToString();
            switch (fixAllState.Scope)
            {
                case CodeFixes.FixAllScope.Project:
                    m[DocumentCount] = fixAllState.Project.DocumentIds.Count;
                    break;

                case CodeFixes.FixAllScope.Solution:
                    m[DocumentCount] = fixAllState.Solution.Projects.Sum(p => p.DocumentIds.Count);
                    break;
            }
        }));
    }

    public static void LogComputationResult(FixAllKind fixAllKind, int correlationId, bool completed, bool timedOut = false)
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

        var functionId = fixAllKind switch
        {
            FixAllKind.CodeFix => FunctionId.CodeFixes_FixAllOccurrencesComputation,
            FixAllKind.Refactoring => FunctionId.Refactoring_FixAllOccurrencesComputation,
            _ => throw ExceptionUtilities.UnexpectedValue(fixAllKind)
        };

        Logger.Log(functionId, KeyValueLogMessage.Create(m =>
        {
            m[CorrelationId] = correlationId;
            m[Result] = value;
        }));
    }

    public static void LogPreviewChangesResult(FixAllKind fixAllKind, int? correlationId, bool applied, bool allChangesApplied = true)
    {
        string value;
        if (applied)
        {
            value = allChangesApplied ? AllChangesApplied : SubsetOfChangesApplied;
        }
        else
        {
            value = Cancelled;
        }

        var functionId = fixAllKind switch
        {
            FixAllKind.CodeFix => FunctionId.CodeFixes_FixAllOccurrencesPreviewChanges,
            FixAllKind.Refactoring => FunctionId.Refactoring_FixAllOccurrencesPreviewChanges,
            _ => throw ExceptionUtilities.UnexpectedValue(fixAllKind)
        };

        Logger.Log(functionId, KeyValueLogMessage.Create(m =>
        {
            // we might not have this info for suppression
            if (correlationId.HasValue)
            {
                m[CorrelationId] = correlationId;
            }

            m[Result] = value;
        }));
    }

    public static void LogDiagnosticsStats(int correlationId, ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap)
    {
        Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Diagnostics, KeyValueLogMessage.Create(m =>
        {
            m[CorrelationId] = correlationId;
            m[DocumentsWithDiagnosticsToFix] = documentsAndDiagnosticsToFixMap.Count;
            m[TotalDiagnosticsToFix] = documentsAndDiagnosticsToFixMap.Values.Sum(v => v.Length);
        }));
    }

    public static void LogDiagnosticsStats(int correlationId, ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectsAndDiagnosticsToFixMap)
    {
        Logger.Log(FunctionId.CodeFixes_FixAllOccurrencesComputation_Project_Diagnostics, KeyValueLogMessage.Create(m =>
        {
            m[CorrelationId] = correlationId;
            m[ProjectsWithDiagnosticsToFix] = projectsAndDiagnosticsToFixMap.Count;
            m[TotalDiagnosticsToFix] = projectsAndDiagnosticsToFixMap.Values.Sum(v => v.Length);
        }));
    }

    public static void LogFixesToMergeStats(FunctionId functionId, int correlationId, int count)
    {
        Logger.Log(functionId, KeyValueLogMessage.Create(m =>
        {
            m[CorrelationId] = correlationId;
            m[TotalFixesToMerge] = count;
        }));
    }

    public static LogMessage CreateCorrelationLogMessage(int correlationId)
        => KeyValueLogMessage.Create(LogType.UserAction, static (m, correlationId) => m[CorrelationId] = correlationId, correlationId);
}
