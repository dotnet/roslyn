' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Text

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Class DiagnosticAnalyzer
    ' Implementing syntax node analyzer because the make const diagnostics in one method body are not dependent on the contents of other method bodies.
    Inherits Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer

    Public Const MakeConstDiagnosticId As String = "MakeConstVB"
    Public Shared ReadOnly MakeConstRule As New DiagnosticDescriptor(MakeConstDiagnosticId,
                                                                     "Make Const",
                                                                     "Can be made const",
                                                                      "Usage",
                                                                     DiagnosticSeverity.Warning,
                                                                     isEnabledByDefault:=True)

    Public Overrides Sub Initialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.LocalDeclarationStatement)
    End Sub

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(MakeConstRule)
        End Get
    End Property

    Private Function CanBeMadeConst(localDeclaration As LocalDeclarationStatementSyntax, semanticModel As SemanticModel) As Boolean
        ' Only consider local variable declarations that are Dim (no Static or Const).
        If Not localDeclaration.Modifiers.All(Function(m) m.VBKind() = SyntaxKind.DimKeyword) Then
            Return False
        End If

        ' Ensure that all variable declarators in the local declaration have
        ' initializers and a single variable name. Additionally, ensure that
        ' each variable is assigned with a constant value.
        For Each declarator In localDeclaration.Declarators
            If declarator.Initializer Is Nothing OrElse declarator.Names.Count <> 1 Then
                Return False
            End If

            If Not semanticModel.GetConstantValue(declarator.Initializer.Value).HasValue Then
                Return False
            End If
        Next

        ' Perform data flow analysis on the local declaration.
        Dim dataFlowAnalysis = semanticModel.AnalyzeDataFlow(localDeclaration)

        ' Retrieve the local symbol for each variable in the local declaration
        ' and ensure that it is not written outside the data flow analysis region.
        For Each declarator In localDeclaration.Declarators
            Dim variable = declarator.Names.Single()
            Dim variableSymbol = semanticModel.GetDeclaredSymbol(variable)
            If dataFlowAnalysis.WrittenOutside.Contains(variableSymbol) Then
                Return False
            End If
        Next

        Return True
    End Function

    Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
        If CanBeMadeConst(CType(context.Node, LocalDeclarationStatementSyntax), context.SemanticModel) Then
            context.ReportDiagnostic(Diagnostic.Create(MakeConstRule, context.Node.GetLocation()))
        End If
    End Sub
End Class
