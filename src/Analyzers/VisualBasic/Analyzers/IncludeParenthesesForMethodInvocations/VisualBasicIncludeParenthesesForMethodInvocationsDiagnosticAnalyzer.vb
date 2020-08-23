' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IncludeParenthesesForMethodInvocations
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicIncludeParenthesesForMethodInvocationsDiagnosticAnalyzer
        Inherits AbstractBuiltInCodeStyleDiagnosticAnalyzer

        Private Shared ReadOnly s_title As New LocalizableResourceString(
            NameOf(VisualBasicAnalyzersResources.Remove_method_invocation_parentheses), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources))

        Private Shared ReadOnly s_removeParenthesesDescriptor As New DiagnosticDescriptor(
            IDEDiagnosticIds.IncludeParenthesesForMethodInvocationsDiagnosticId,
            s_title,
            messageFormat:=s_title, DiagnosticCategory.Style, DiagnosticSeverity.Hidden, isEnabledByDefault:=True)

        Public Sub New()
            MyBase.New(
                diagnosticId:=IDEDiagnosticIds.IncludeParenthesesForMethodInvocationsDiagnosticId,
                [option]:=VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations,
                language:=LanguageNames.VisualBasic,
                title:=New LocalizableResourceString(NameOf(VisualBasicAnalyzersResources.Add_method_invocation_parentheses), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
        End Sub

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterOperationAction(AddressOf AnalyzeInvocation, OperationKind.Invocation)
        End Sub

        Private Sub AnalyzeInvocation(context As OperationAnalysisContext)
            Dim node = context.Operation.Syntax
            Dim invocationExpression = TryCast(node, InvocationExpressionSyntax)
            If invocationExpression Is Nothing Then
                Return
            End If

            Dim includeParentheses = context.Options.GetOption(VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations, node.SyntaxTree, context.CancellationToken).Value
            If includeParentheses Then
                If invocationExpression.ArgumentList Is Nothing Then
                    ' Parentheses doesn't exist, ex: Dim x = FunctionCall
                    '                                        ^^^^^^^^^^^^
                    context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor, node.GetLocation(), ReportDiagnostic.Hidden, ' Will the hidden severity here overwrite any user preference?
                        additionalLocations:={node.GetLocation()}, properties:=Nothing))
                End If
            Else
                If invocationExpression.ArgumentList IsNot Nothing AndAlso Not invocationExpression.ArgumentList.Arguments.Any() Then
                    ' Report Only if there are parentheses (ArgumentList not null) And if there are 0 arguments.
                    ' TODO: Check if I've to check context.Options.GetOption(.....).Notification.Severity and report Unnecessary or the default Descriptor.
                    context.ReportDiagnostic(DiagnosticHelper.Create(s_removeParenthesesDescriptor, invocationExpression.ArgumentList.GetLocation(), ReportDiagnostic.Hidden,
                        additionalLocations:={node.GetLocation()}, properties:=Nothing))
                End If
            End If
        End Sub

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function
    End Class
End Namespace
