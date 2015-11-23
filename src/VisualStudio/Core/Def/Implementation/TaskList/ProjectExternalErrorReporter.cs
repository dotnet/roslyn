// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal class ProjectExternalErrorReporter : IVsReportExternalErrors, IVsLanguageServiceBuildErrorReporter2
    {
        internal static readonly ImmutableDictionary<string, string> Properties = ImmutableDictionary<string, string>.Empty.Add(WellKnownDiagnosticPropertyNames.Origin, WellKnownDiagnosticTags.Build);
        internal static readonly IReadOnlyList<string> CustomTags = ImmutableArray.Create(WellKnownDiagnosticTags.Telemetry);
        internal static readonly IReadOnlyList<string> CompilerDiagnosticCustomTags = ImmutableArray.Create(WellKnownDiagnosticTags.Compiler, WellKnownDiagnosticTags.Telemetry);

        private readonly ProjectId _projectId;
        private readonly string _errorCodePrefix;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly ExternalErrorDiagnosticUpdateSource _diagnosticProvider;

        public ProjectExternalErrorReporter(ProjectId projectId, string errorCodePrefix, IServiceProvider serviceProvider)
        {
            _projectId = projectId;
            _errorCodePrefix = errorCodePrefix;
            _diagnosticProvider = serviceProvider.GetMefService<ExternalErrorDiagnosticUpdateSource>();
            _workspace = serviceProvider.GetMefService<VisualStudioWorkspaceImpl>();

            Debug.Assert(_diagnosticProvider != null);
            Debug.Assert(_workspace != null);
        }

        public int AddNewErrors(IVsEnumExternalErrors pErrors)
        {
            var projectErrors = new HashSet<DiagnosticData>();
            var documentErrorsMap = new Dictionary<DocumentId, HashSet<DiagnosticData>>();

            var errors = new ExternalError[1];
            uint fetched;
            while (pErrors.Next(1, errors, out fetched) == VSConstants.S_OK && fetched == 1)
            {
                var error = errors[0];

                DiagnosticData diagnostic;
                if (error.bstrFileName != null)
                {
                    diagnostic = CreateDocumentDiagnosticItem(error);
                    if (diagnostic != null)
                    {
                        var diagnostics = documentErrorsMap.GetOrAdd(diagnostic.DocumentId, _ => new HashSet<DiagnosticData>());
                        diagnostics.Add(diagnostic);
                        continue;
                    }

                    projectErrors.Add(CreateProjectDiagnosticItem(error));
                }
                else
                {
                    projectErrors.Add(CreateProjectDiagnosticItem(error));
                }
            }

            _diagnosticProvider.AddNewErrors(_projectId, projectErrors, documentErrorsMap);
            return VSConstants.S_OK;
        }

        public int ClearAllErrors()
        {
            _diagnosticProvider.ClearErrors(_projectId);
            return VSConstants.S_OK;
        }

        public int GetErrors(out IVsEnumExternalErrors pErrors)
        {
            pErrors = null;
            Debug.Fail("This is not implemented, because no one called it.");
            return VSConstants.E_NOTIMPL;
        }

        private DiagnosticData CreateProjectDiagnosticItem(ExternalError error)
        {
            return GetDiagnosticData(error);
        }

        private DiagnosticData CreateDocumentDiagnosticItem(ExternalError error)
        {
            var hostProject = _workspace.GetHostProject(_projectId);
            if (!hostProject.ContainsFile(error.bstrFileName))
            {
                return null;
            }

            var hostDocument = hostProject.GetCurrentDocumentFromPath(error.bstrFileName);

            var line = error.iLine;
            var column = error.iCol;
            var containedDocument = hostDocument as ContainedDocument;
            if (containedDocument != null)
            {
                var span = new VsTextSpan
                {
                    iStartLine = line,
                    iStartIndex = column,
                    iEndLine = line,
                    iEndIndex = column,
                };

                var spans = new VsTextSpan[1];
                Marshal.ThrowExceptionForHR(containedDocument.ContainedLanguage.BufferCoordinator.MapPrimaryToSecondarySpan(
                    span,
                    spans));

                line = spans[0].iStartLine;
                column = spans[0].iStartIndex;
            }

            return GetDiagnosticData(error, hostDocument.Id, line, column);
        }

        public int ReportError(string bstrErrorMessage, string bstrErrorId, [ComAliasName("VsShell.VSTASKPRIORITY")]VSTASKPRIORITY nPriority, int iLine, int iColumn, string bstrFileName)
        {
            ReportError2(bstrErrorMessage, bstrErrorId, nPriority, iLine, iColumn, iLine, iColumn, bstrFileName);
            return VSConstants.S_OK;
        }

        // TODO: Use PreserveSig instead of throwing these exceptions for common cases.
        public void ReportError2(string bstrErrorMessage, string bstrErrorId, [ComAliasName("VsShell.VSTASKPRIORITY")]VSTASKPRIORITY nPriority, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn, string bstrFileName)
        {
            if ((iEndLine >= 0 && iEndColumn >= 0) &&
                ((iEndLine < iStartLine) ||
                 (iEndLine == iStartLine && iEndColumn < iStartColumn)))
            {
                throw new ArgumentException(ServicesVSResources.EndPositionMustBeGreaterThanStart);
            }

            // We only handle errors that have positions.  For the rest, we punt back to the 
            // project system.
            if (iStartLine < 0 || iStartColumn < 0)
            {
                throw new NotImplementedException();
            }

            var hostProject = _workspace.GetHostProject(_projectId);
            if (!hostProject.ContainsFile(bstrFileName))
            {
                throw new NotImplementedException();
            }

            var hostDocument = hostProject.GetCurrentDocumentFromPath(bstrFileName);

            var priority = (VSTASKPRIORITY)nPriority;
            DiagnosticSeverity severity;
            switch (priority)
            {
                case VSTASKPRIORITY.TP_HIGH:
                    severity = DiagnosticSeverity.Error;
                    break;
                case VSTASKPRIORITY.TP_NORMAL:
                    severity = DiagnosticSeverity.Warning;
                    break;
                case VSTASKPRIORITY.TP_LOW:
                    severity = DiagnosticSeverity.Info;
                    break;
                default:
                    throw new ArgumentException(ServicesVSResources.NotAValidValue, "nPriority");
            }

            var diagnostic = GetDiagnosticData(
                hostDocument.Id, bstrErrorId, bstrErrorMessage, severity,
                null, iStartLine, iStartColumn, iEndLine, iEndColumn,
                bstrFileName, iStartLine, iStartColumn, iEndLine, iEndColumn);

            _diagnosticProvider.AddNewErrors(hostDocument.Id, diagnostic);
        }

        public int ClearErrors()
        {
            _diagnosticProvider.ClearErrors(_projectId);
            return VSConstants.S_OK;
        }

        private string GetErrorId(ExternalError error)
        {
            return string.Format("{0}{1:0000}", _errorCodePrefix, error.iErrorID);
        }

        private static int GetWarningLevel(DiagnosticSeverity severity)
        {
            return severity == DiagnosticSeverity.Error ? 0 : 1;
        }

        private static DiagnosticSeverity GetDiagnosticSeverity(ExternalError error)
        {
            return error.fError != 0 ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
        }

        private DiagnosticData GetDiagnosticData(
            ExternalError error, DocumentId id = null, int line = 0, int column = 0)
        {
            if (id != null)
            {
                // save error line/column (surface buffer location) as mapped line/column so that we can display
                // right location on closed Venus file.
                return GetDiagnosticData(
                    id, GetErrorId(error), error.bstrText, GetDiagnosticSeverity(error),
                    null, error.iLine, error.iCol, error.iLine, error.iCol, error.bstrFileName, line, column, line, column);
            }

            return GetDiagnosticData(
                id, GetErrorId(error), error.bstrText, GetDiagnosticSeverity(error), null, 0, 0, 0, 0, null, 0, 0, 0, 0);
        }

        private static bool IsCompilerDiagnostic(string errorId)
        {
            if (!string.IsNullOrEmpty(errorId) && errorId.Length > 2)
            {
                var prefix = errorId.Substring(0, 2);
                if (prefix.Equals("CS", StringComparison.OrdinalIgnoreCase) || prefix.Equals("BC", StringComparison.OrdinalIgnoreCase))
                {
                    var suffix = errorId.Substring(2);
                    int id;
                    return int.TryParse(suffix, out id);
                }
            }

            return false;
        }

        private static IReadOnlyList<string> GetCustomTags(string errorId)
        {
            return IsCompilerDiagnostic(errorId) ? CompilerDiagnosticCustomTags : CustomTags;
        }

        private DiagnosticData GetDiagnosticData(
            DocumentId id, string errorId, string message, DiagnosticSeverity severity,
            string mappedFilePath, int mappedStartLine, int mappedStartColumn, int mappedEndLine, int mappedEndColumn,
            string originalFilePath, int originalStartLine, int originalStartColumn, int originalEndLine, int originalEndColumn)
        {
            return new DiagnosticData(
                id: errorId,
                category: WellKnownDiagnosticTags.Build,
                message: message,
                title: message,
                enuMessageForBingSearch: message, // Unfortunately, there is no way to get ENU text for this since this is an external error.
                severity: severity,
                defaultSeverity: severity,
                isEnabledByDefault: true,
                warningLevel: GetWarningLevel(severity),
                customTags: GetCustomTags(errorId),
                properties: Properties,
                workspace: _workspace,
                projectId: _projectId,
                location: new DiagnosticDataLocation(id,
                    sourceSpan: null,
                    originalFilePath: originalFilePath,
                    originalStartLine: originalStartLine,
                    originalStartColumn: originalStartColumn,
                    originalEndLine: originalEndLine,
                    originalEndColumn: originalEndColumn,
                    mappedFilePath: mappedFilePath,
                    mappedStartLine: mappedStartLine,
                    mappedStartColumn: mappedStartColumn,
                    mappedEndLine: mappedEndLine,
                    mappedEndColumn: mappedEndColumn));
        }
    }
}
