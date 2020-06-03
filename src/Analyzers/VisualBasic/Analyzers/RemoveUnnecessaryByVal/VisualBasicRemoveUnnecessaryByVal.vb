' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Friend Class VisualBasicRemoveUnnecessaryByVal
    Inherits DiagnosticAnalyzer
    Implements IBuiltInAnalyzer ' Should I implement IBuiltInAnalyzer??

    Friend Const DiagnosticFixableId As String = "RemoveUnnecessaryByVal"
    Private Shared ReadOnly s_descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(id:="RemoveUnnecessaryByVal",
                                                                                            title:="",
                                                                                            messageFormat:="",
                                                                                            category:="",
                                                                                            defaultSeverity:=DiagnosticSeverity.Hidden,
                                                                                            isEnabledByDefault:=True,
                                                                                            description:="",
                                                                                            helpLinkUri:="",
                                                                                            WellKnownDiagnosticTags.Unnecessary)

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(s_descriptor)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.EnableConcurrentExecution()
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze Or GeneratedCodeAnalysisFlags.ReportDiagnostics)
        context.RegisterSyntaxNodeAction(
            Sub(syntaxContext As SyntaxNodeAnalysisContext)
                syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                                               s_descriptor, syntaxContext.Node.GetLocation(), ReportDiagnostic.Hidden, Nothing, Nothing))
            End Sub, SyntaxKind.ByValKeyword)
    End Sub

    Public Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory Implements IBuiltInAnalyzer.GetAnalyzerCategory
        ' What should be returned here?
        Return DiagnosticAnalyzerCategory.None
    End Function

    Public Function OpenFileOnly(options As AnalyzerConfigOptions) As Boolean Implements IBuiltInAnalyzer.OpenFileOnly
        Return False ' Is that correct? I don't know how OpenFileOnly exactly works, but removing ByVal can be done in all files not only the open one.
    End Function
End Class
