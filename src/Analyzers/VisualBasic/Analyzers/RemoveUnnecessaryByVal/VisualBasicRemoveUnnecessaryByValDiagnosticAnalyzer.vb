' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRemoveUnnecessaryByValDiagnosticAnalyzer
        Inherits AbstractCodeStyleDiagnosticAnalyzer

        Private Shared ReadOnly s_descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(
            id:=IDEDiagnosticIds.RemoveUnnecessaryByValDiagnosticId,
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

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(
                Sub(syntaxContext As SyntaxNodeAnalysisContext)
                    Dim modifiers = CType(syntaxContext.Node, ParameterSyntax).Modifiers
                    For Each modifier In modifiers
                        If modifier.IsKind(SyntaxKind.ByValKeyword) Then
                            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                                s_descriptor, modifier.GetLocation(), ReportDiagnostic.Hidden, additionalLocations:=Nothing, properties:=Nothing))
                        End If
                    Next

                End Sub, SyntaxKind.Parameter)
        End Sub
    End Class
End Namespace
