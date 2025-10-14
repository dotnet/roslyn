// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

internal sealed class ProjectExternalErrorReporter(
    ProjectId projectId,
    Guid projectHierarchyGuid,
    string errorCodePrefix,
    string language,
    VisualStudioWorkspaceImpl workspace) : IVsReportExternalErrors, IVsLanguageServiceBuildErrorReporter2
{
    internal static readonly ImmutableArray<string> CustomTags = [WellKnownDiagnosticTags.Telemetry];
    internal static readonly ImmutableArray<string> CompilerDiagnosticCustomTags = [WellKnownDiagnosticTags.Compiler, WellKnownDiagnosticTags.Telemetry];

    private readonly ProjectId _projectId = projectId;

    /// <summary>
    /// Passed to the error reporting service to allow current project error list filters to work.
    /// </summary>
    private readonly Guid _projectHierarchyGuid = projectHierarchyGuid;
    private readonly string _errorCodePrefix = errorCodePrefix;
    private readonly string _language = language;

    private readonly VisualStudioWorkspaceImpl _workspace = workspace;

    private ExternalErrorDiagnosticUpdateSource DiagnosticProvider => _workspace.ExternalErrorDiagnosticUpdateSource;

    private static ImmutableArray<ExternalError> GetExternalErrors(IVsEnumExternalErrors pErrors)
    {
        using var _ = ArrayBuilder<ExternalError>.GetInstance(out var allErrors);

        var errors = new ExternalError[1];
        while (pErrors.Next(1, errors, out var fetched) == VSConstants.S_OK && fetched == 1)
        {
            var error = errors[0];
            allErrors.Add(error);
        }

        return allErrors.ToImmutableAndClear();
    }

    public int AddNewErrors(IVsEnumExternalErrors pErrors)
    {
        var solution = _workspace.CurrentSolution;
        var project = solution.GetProject(_projectId);
        if (project != null)
        {
            AddNewErrors(project, GetExternalErrors(pErrors));
        }

        return VSConstants.S_OK;
    }

    private void AddNewErrors(Project project, ImmutableArray<ExternalError> allErrors)
    {
        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var allDiagnostics);

        var solution = project.Solution;
        var errorIds = allErrors.Select(e => e.iErrorID).Distinct().Select(GetErrorId).ToImmutableArray();

        foreach (var error in allErrors)
        {
            if (error.bstrFileName != null)
            {
                var diagnostic = TryCreateDocumentDiagnosticItem(project, error);
                if (diagnostic != null)
                {
                    allDiagnostics.Add(diagnostic);
                    continue;
                }
            }

            allDiagnostics.Add(GetDiagnosticData(
                project,
                documentId: null,
                GetErrorId(error),
                error.bstrText,
                GetDiagnosticSeverity(error),
                _language,
                new FileLinePositionSpan(project.FilePath ?? "", span: default)));
        }

        DiagnosticProvider.AddNewErrors(_projectId, _projectHierarchyGuid, allDiagnostics.ToImmutableAndClear());
    }

    public int ClearAllErrors()
        => ClearErrorsWorker();

    public int ClearErrors()
        => ClearErrorsWorker();

    private int ClearErrorsWorker()
    {
        DiagnosticProvider.ClearErrors(_projectId);
        return VSConstants.S_OK;
    }

    public int GetErrors(out IVsEnumExternalErrors? pErrors)
    {
        pErrors = null;
        Debug.Fail("This is not implemented, because no one called it.");
        return VSConstants.E_NOTIMPL;
    }

    private DocumentId? TryGetDocumentId(string filePath)
    {
        return _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath)
                         .Where(f => f.ProjectId == _projectId)
                         .FirstOrDefault();
    }

    private DiagnosticData? TryCreateDocumentDiagnosticItem(
        Project project,
        ExternalError error)
    {
        var documentId = TryGetDocumentId(error.bstrFileName);
        if (documentId == null)
        {
            return null;
        }

        var line = error.iLine;
        var column = error.iCol;

        // something we should move to document service one day. but until then, we keep the old way.
        // build basically output error location on surface buffer and we map it back to
        // subject buffer for contained document. so that contained document can map
        // it back to surface buffer when navigate. whole problem comes in due to the mapped span.
        // unlike live error, build outputs mapped span and we save it as original span (since we
        // have no idea whether it is mapped or not). for contained document case, we do know it is
        // mapped span, so we calculate original span and put that in original span.
        var containedDocument = ContainedDocument.TryGetContainedDocument(documentId);
        if (containedDocument != null)
        {
            var span = new TextManager.Interop.TextSpan
            {
                iStartLine = line,
                iStartIndex = column,
                iEndLine = line,
                iEndIndex = column,
            };

            var spans = new TextManager.Interop.TextSpan[1];
            Marshal.ThrowExceptionForHR(containedDocument.BufferCoordinator.MapPrimaryToSecondarySpan(
                span,
                spans));

            line = spans[0].iStartLine;
            column = spans[0].iStartIndex;
        }

        // save error line/column (surface buffer location) as mapped line/column so that we can display
        // right location on closed Venus file.
        return GetDiagnosticData(
            project,
            documentId,
            GetErrorId(error),
            error.bstrText,
            GetDiagnosticSeverity(error),
            _language,
            new FileLinePositionSpan(error.bstrFileName,
                new LinePosition(line, column),
                new LinePosition(line, column)));
    }

    public int ReportError(string bstrErrorMessage, string bstrErrorId, [ComAliasName("VsShell.VSTASKPRIORITY")] VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
    {
        ReportError2(bstrErrorMessage, bstrErrorId, nPriority, iLine, iColumn, iLine, iColumn, bstrFileName);
        return VSConstants.S_OK;
    }

    // TODO: Use PreserveSig instead of throwing these exceptions for common cases.
    public void ReportError2(
        string bstrErrorMessage,
        string bstrErrorId,
        [ComAliasName("VsShell.VSTASKPRIORITY")] VSTASKPRIORITY nPriority,
        int iStartLine,
        int iStartColumn,
        int iEndLine,
        int iEndColumn,
        string bstrFileName)
    {

        // make sure we have error id, otherwise, we simple don't support
        // this error
        if (bstrErrorId == null)
        {
            // record NFW to see who violates contract.
            FatalError.ReportAndCatch(new Exception("errorId is null"));
            return;
        }

        if (!bstrErrorId.StartsWith(_errorCodePrefix))
            return;

        if ((iEndLine >= 0 && iEndColumn >= 0) &&
           ((iEndLine < iStartLine) ||
            (iEndLine == iStartLine && iEndColumn < iStartColumn)))
        {
            throw new ArgumentException(ServicesVSResources.End_position_must_be_start_position);
        }

        var severity = nPriority switch
        {
            VSTASKPRIORITY.TP_HIGH => DiagnosticSeverity.Error,
            VSTASKPRIORITY.TP_NORMAL => DiagnosticSeverity.Warning,
            VSTASKPRIORITY.TP_LOW => DiagnosticSeverity.Info,
            _ => throw new ArgumentException(ServicesVSResources.Not_a_valid_value, nameof(nPriority))
        };

        DocumentId? documentId;
        if (iStartLine < 0 || iStartColumn < 0)
        {
            documentId = null;
            iStartLine = iStartColumn = iEndLine = iEndColumn = 0;
        }
        else
        {
            documentId = TryGetDocumentId(bstrFileName);
        }

        if (iEndLine < 0)
            iEndLine = iStartLine;
        if (iEndColumn < 0)
            iEndColumn = iStartColumn;

        var solution = _workspace.CurrentSolution;
        var project = solution.GetProject(_projectId);
        if (project is null)
            return;

        var diagnostic = GetDiagnosticData(
            project,
            documentId,
            bstrErrorId,
            bstrErrorMessage,
            severity,
            _language,
            new FileLinePositionSpan(
                bstrFileName,
                new LinePosition(iStartLine, iStartColumn),
                new LinePosition(iEndLine, iEndColumn)));

        DiagnosticProvider.AddNewErrors(_projectId, _projectHierarchyGuid, [diagnostic]);
    }

    private static DiagnosticData GetDiagnosticData(
        Project project,
        DocumentId? documentId,
        string errorId,
        string message,
        DiagnosticSeverity severity,
        string language,
        FileLinePositionSpan unmappedSpan)
    {
        return new DiagnosticData(
            id: errorId,
            category: WellKnownDiagnosticTags.Build,
            message: message,
            title: message,
            description: message,
            severity: severity,
            defaultSeverity: severity,
            isEnabledByDefault: true,
            warningLevel: severity == DiagnosticSeverity.Error ? 0 : 1,
            customTags: IsCompilerDiagnostic(errorId) ? CompilerDiagnosticCustomTags : CustomTags,
            properties: DiagnosticData.PropertiesForBuildDiagnostic,
            projectId: project.Id,
            location: new DiagnosticDataLocation(
                unmappedSpan,
                documentId),
            language: language);
    }

    private static bool IsCompilerDiagnostic(string errorId)
    {
        if (!string.IsNullOrEmpty(errorId) && errorId.Length > 2)
        {
            var prefix = errorId[..2];
            if (prefix.Equals("CS", StringComparison.OrdinalIgnoreCase) || prefix.Equals("BC", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = errorId[2..];
                return int.TryParse(suffix, out _);
            }
        }

        return false;
    }

    private string GetErrorId(ExternalError error)
        => GetErrorId(error.iErrorID);

    private string GetErrorId(int errorId)
        => string.Format("{0}{1:0000}", _errorCodePrefix, errorId);

    private static DiagnosticSeverity GetDiagnosticSeverity(ExternalError error)
        => error.fError != 0 ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
}
