' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(IDiagnosticIdToEditorConfigOptionMappingService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicDiagnosticIdToEditorConfigOptionMappingService
        Inherits AbstractDiagnosticIdToEditorConfigOptionMappingService

        Protected Overrides Function TryGetLanguageSpecificOption(diagnosticId As String) As (optionOpt As IOption, handled As Boolean)
            Dim [option] As IOption

            Select Case diagnosticId
                Case IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId
                    [option] = VisualBasicCodeStyleOptions.UnusedValueExpressionStatement
                Case IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId
                    [option] = VisualBasicCodeStyleOptions.UnusedValueAssignment

                ' C# specific Diagnostic IDs
                Case IDEDiagnosticIds.AddBracesDiagnosticId,
                     IDEDiagnosticIds.InlineAsTypeCheckId,
                     IDEDiagnosticIds.InlineIsTypeCheckId,
                     IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                     IDEDiagnosticIds.UseDefaultLiteralDiagnosticId,
                     IDEDiagnosticIds.UseLocalFunctionDiagnosticId,
                     IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId,
                     IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId,
                     IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
                     IDEDiagnosticIds.UseRangeOperatorDiagnosticId,
                     IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId,
                     IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId,
                     IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId,
                     IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId
                    [option] = Nothing

                Case Else
                    Return (optionOpt:=Nothing, handled:=False)
            End Select

            Return (optionOpt:=[option], handled:=True)
        End Function
    End Class
End Namespace
