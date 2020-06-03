' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRemoveUnnecessaryByVal
        Inherits AbstractCodeStyleDiagnosticAnalyzer

        Friend Const DiagnosticFixableId As String = IDEDiagnosticIds.RemoveUnnecessaryByValDiagnosticId
        Private Shared ReadOnly s_descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(
            id:=DiagnosticFixableId,
            title:="",
            messageFormat:="",
            category:="",
            defaultSeverity:=DiagnosticSeverity.Hidden,
            isEnabledByDefault:=True,
            description:="",
            helpLinkUri:="",
            WellKnownDiagnosticTags.Unnecessary)

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(s_descriptor)

        Public Sub New()
            MyBase.New(s_descriptor.Id, s_descriptor.Title, s_descriptor.MessageFormat)
        End Sub

        Protected Overrides Sub InitializeWorker(ByVal context As AnalysisContext)
            context.EnableConcurrentExecution()
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze Or GeneratedCodeAnalysisFlags.ReportDiagnostics)

            context.RegisterSyntaxNodeAction(
            Sub(syntaxContext As SyntaxNodeAnalysisContext)
                Dim location = GetByValLocation(CType(syntaxContext.Node, ParameterSyntax).Modifiers)
                If location IsNot Nothing Then
                    syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                                               s_descriptor, location, ReportDiagnostic.Hidden, additionalLocations:=Nothing, properties:=Nothing))
                End If
            End Sub, SyntaxKind.Parameter)
        End Sub

        Private Function GetByValLocation(modifiers As SyntaxTokenList) As Location
            For Each modifier In modifiers
                If modifier.IsKind(SyntaxKind.ByValKeyword) Then
                    Return modifier.GetLocation()
                End If
            Next
            Return Nothing
        End Function
    End Class
End Namespace
