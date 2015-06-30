﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Applies Visual Basic-specific modification and filtering of <see cref="Diagnostic"/>s.
    ''' </summary>
    Friend Class VisualBasicDiagnosticFilter
        Private Shared ReadOnly s_alinkWarnings As ERRID() = {ERRID.WRN_ConflictingMachineAssembly,
                                                            ERRID.WRN_RefCultureMismatch,
                                                            ERRID.WRN_InvalidVersionFormat}

        ''' <summary>
        ''' Modifies an input <see cref="Diagnostic"/> per the given options. For example, the
        ''' severity may be escalated, or the <see cref="Diagnostic"/> may be filtered out entirely
        ''' (by returning null).
        ''' </summary>
        ''' <param name="diagnostic">The input diagnostic</param>
        ''' <param name="generalDiagnosticOption">How warning diagnostics should be reported</param>
        ''' <param name="specificDiagnosticOptions">How specific diagnostics should be reported</param>
        ''' <returns>A diagnostic updated to reflect the options, or null if it has been filtered out</returns>
        Public Shared Function Filter(diagnostic As Diagnostic, generalDiagnosticOption As ReportDiagnostic, specificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic)) As Diagnostic
            ' Diagnostic ids must be processed in case-insensitive fashion in VB.
            Dim caseInsensitiveSpecificDiagnosticOptions =
            ImmutableDictionary.Create(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer).AddRange(specificDiagnosticOptions)

            ' Filter void diagnostics so that our callers don't have to perform resolution
            ' (which might copy the list of diagnostics).
            If diagnostic.Severity = InternalDiagnosticSeverity.Void Then
                Return Nothing
            End If

            ' If diagnostic is not configurable, keep it as it is.
            If diagnostic.IsNotConfigurable Then
                If diagnostic.IsEnabledByDefault Then
                    ' Enabled NotConfigurable should always be reported as it is.
                    Return diagnostic
                Else
                    ' Disabled NotConfigurable should never be reported.
                    Return Nothing
                End If
            End If

            ' In the native compiler, all warnings originating from alink.dll were issued
            ' under the id WRN_ALinkWarn - 1607. If nowarn:1607 is used they would get
            ' none of those warnings. In Roslyn, we've given each of these warnings their
            ' own number, so that they may be configured independently. To preserve compatibility
            ' if a user has specifically configured 1607 And we are reporting one of the alink warnings, use
            ' the configuration specified for 1607. As implemented, this could result in 
            ' specifying warnaserror:1607 And getting a message saying "warning as error CS8012..."
            ' We don't permit configuring 1607 and independently configuring the new warnings.

            Dim report As ReportDiagnostic

            If (s_alinkWarnings.Contains(CType(diagnostic.Code, ERRID)) AndAlso
                caseInsensitiveSpecificDiagnosticOptions.Keys.Contains(VisualBasic.MessageProvider.Instance.GetIdForErrorCode(ERRID.WRN_AssemblyGeneration1))) Then
                report = GetDiagnosticReport(VisualBasic.MessageProvider.Instance.GetSeverity(ERRID.WRN_AssemblyGeneration1),
                diagnostic.IsEnabledByDefault,
                VisualBasic.MessageProvider.Instance.GetIdForErrorCode(ERRID.WRN_AssemblyGeneration1),
                diagnostic.Location,
                diagnostic.Category,
                generalDiagnosticOption,
                caseInsensitiveSpecificDiagnosticOptions)
            Else
                report = GetDiagnosticReport(diagnostic.Severity, diagnostic.IsEnabledByDefault, diagnostic.Id, diagnostic.Location, diagnostic.Category, generalDiagnosticOption, caseInsensitiveSpecificDiagnosticOptions)
            End If

            Return diagnostic.WithReportDiagnostic(report)
        End Function

        Friend Shared Function GetDiagnosticReport(severity As DiagnosticSeverity, isEnabledByDefault As Boolean, id As String, location As Location, category As String, generalDiagnosticOption As ReportDiagnostic, caseInsensitiveSpecificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic)) As ReportDiagnostic
            ' Read options (e.g., /nowarn or /warnaserror)
            Dim report As ReportDiagnostic = ReportDiagnostic.Default
            Dim isSpecified = caseInsensitiveSpecificDiagnosticOptions.TryGetValue(id, report)
            If Not isSpecified Then
                report = If(isEnabledByDefault, ReportDiagnostic.Default, ReportDiagnostic.Suppress)
            End If

            ' Compute if the reporting should be suppressed.
            If report = ReportDiagnostic.Suppress Then
                Return ReportDiagnostic.Suppress
            End If

            ' If location is available, check warning directive state.
            If location IsNot Nothing AndAlso location.SourceTree IsNot Nothing AndAlso
           location.SourceTree.GetWarningState(id, location.SourceSpan.Start) = ReportDiagnostic.Suppress Then
                Return ReportDiagnostic.Suppress
            End If

            ' check options (/nowarn)
            ' When doing suppress-all-warnings, don't lower severity for anything other than warning and info.
            ' We shouldn't suppress hidden diagnostics here because then features that use hidden diagnostics to
            ' display light bulb would stop working if someone has suppress-all-warnings (/nowarn) specified in their project.
            If generalDiagnosticOption = ReportDiagnostic.Suppress AndAlso
            (severity = DiagnosticSeverity.Warning OrElse severity = DiagnosticSeverity.Info) Then
                Return ReportDiagnostic.Suppress
            End If

            ' check the AllWarningsAsErrors flag and the specific lists from /warnaserror[+|-] option.
            ' If we've been asked to do warn-as-error then don't raise severity for anything below warning (info or hidden).
            If (generalDiagnosticOption = ReportDiagnostic.Error) AndAlso (severity = DiagnosticSeverity.Warning) Then
                ' In the case for both /warnaserror and /warnaserror-:<n> at the same time,
                ' do not report it as an error.
                ' If there has been no specific action for this warning, then turn it into an error.
                If (Not isSpecified) AndAlso (report = ReportDiagnostic.Default) Then
                    Return ReportDiagnostic.Error
                End If
            End If

            Return report

        End Function
    End Class
End Namespace
