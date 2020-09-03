' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ParenthesesForMethodInvocations
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicParenthesesStyleDiagnosticAnalyzer
        Inherits AbstractBuiltInCodeStyleDiagnosticAnalyzer

        Private Shared ReadOnly s_titleForAddParentheses As New LocalizableResourceString(
            NameOf(VisualBasicAnalyzersResources.Add_parentheses_to_method_invocation),
            VisualBasicAnalyzersResources.ResourceManager,
            GetType(VisualBasicAnalyzersResources))

        Private Shared ReadOnly s_titleForRemoveParentheses As New LocalizableResourceString(
            NameOf(VisualBasicAnalyzersResources.Remove_parentheses_from_method_invocation),
            VisualBasicAnalyzersResources.ResourceManager,
            GetType(VisualBasicAnalyzersResources))

        Protected Sub New()
            MyBase.New(GetSupportedDescriptorsWithOptions(), LanguageNames.VisualBasic)
        End Sub

        Private Shared Function GetSupportedDescriptorsWithOptions() As ImmutableDictionary(Of DiagnosticDescriptor, Options.ILanguageSpecificOption)
            Dim builder = ImmutableDictionary.CreateBuilder(Of DiagnosticDescriptor, Options.ILanguageSpecificOption)()
            ' TODO: Rename the DiagnosticId const to something like "ParenthesesStyleForMethodInvocationsDiagnosticId".
            builder.Add(CreateDescriptorWithId(IDEDiagnosticIds.RemoveParenthesesFromMethodInvocationsDiagnosticId, s_titleForAddParentheses, s_titleForAddParentheses), VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations)
            builder.Add(CreateDescriptorWithId(IDEDiagnosticIds.RemoveParenthesesFromMethodInvocationsDiagnosticId, s_titleForRemoveParentheses, s_titleForRemoveParentheses), VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations)
            Return builder.ToImmutable()
        End Function

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterOperationAction(AddressOf AnalyzeInvocation, OperationKind.Invocation)
        End Sub

        Private Sub AnalyzeInvocation(context As OperationAnalysisContext)
            Dim node = context.Operation.Syntax
            Dim invocationExpression = TryCast(node, InvocationExpressionSyntax)
            If invocationExpression Is Nothing Then
                Return
            End If
            Dim [option] = context.Options.GetOption(VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations, node.SyntaxTree, context.CancellationToken)
            Dim descriptor = CreateDescriptorWithId(DescriptorId, _localizableTitle, _localizableMessageFormat)

            If IsViolatingPreference([option].Value, invocationExpression) Then
                context.ReportDiagnostic(DiagnosticHelper.Create(descriptor, node.GetLocation(), [option].Notification.Severity,
                    additionalLocations:=Nothing, properties:=Nothing))
            End If
        End Sub

        Private Shared Function IsViolatingPreference(includeParenthesesPreference As Boolean, invocation As InvocationExpressionSyntax) As Boolean
            If includeParenthesesPreference Then
                ' User wants to include parentheses. Return True indicating violation if parentheses doesn't exist.
                Return invocation.ArgumentList Is Nothing
            Else
                ' User doesn't want to include parentheses. Return True indicating violation of parentheses exist for a method taking 0 arguments.
                Return invocation.ArgumentList IsNot Nothing AndAlso Not invocation.ArgumentList.Arguments.Any()
            End If
        End Function
    End Class
End Namespace
