' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryByValDiagnosticAnalyzer
        Inherits AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(
                diagnosticId:=IDEDiagnosticIds.RemoveUnnecessaryByValDiagnosticId,
                enforceOnBuild:=EnforceOnBuildValues.RemoveUnnecessaryByVal,
                [option]:=Nothing,
                fadingOption:=Nothing,
                title:=New LocalizableResourceString(NameOf(VisualBasicAnalyzersResources.Remove_ByVal), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
        End Sub

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(
                Sub(syntaxContext As SyntaxNodeAnalysisContext)
                    If ShouldSkipAnalysis(syntaxContext, notification:=Nothing) Then
                        Return
                    End If

                    Dim parameterSyntax = DirectCast(syntaxContext.Node, ParameterSyntax)
                    For Each modifier In parameterSyntax.Modifiers
                        If modifier.IsKind(SyntaxKind.ByValKeyword) Then
                            syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptor, modifier.GetLocation(), additionalLocations:={parameterSyntax.GetLocation()}))
                        End If
                    Next
                End Sub, SyntaxKind.Parameter)
        End Sub

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function
    End Class
End Namespace
