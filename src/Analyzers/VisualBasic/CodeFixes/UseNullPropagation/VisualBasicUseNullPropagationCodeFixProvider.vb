' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UseNullPropagation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseNullPropagation), [Shared]>
    Friend NotInheritable Class VisualBasicUseNullPropagationCodeFixProvider
        Inherits AbstractUseNullPropagationCodeFixProvider(Of
            VisualBasicUseNullPropagationDiagnosticAnalyzer,
            SyntaxKind,
            ExpressionSyntax,
            ExecutableStatementSyntax,
            TernaryConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            MultiLineIfBlockSyntax,
            ExpressionStatementSyntax,
            ArgumentListSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property Analyzer As VisualBasicUseNullPropagationDiagnosticAnalyzer = VisualBasicUseNullPropagationDiagnosticAnalyzer.Instance

        Protected Overrides Function PostProcessElseIf(ifStatement As MultiLineIfBlockSyntax, newWhenTrueStatement As ExecutableStatementSyntax) As SyntaxNode
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function ElementBindingExpression(argumentList As ArgumentListSyntax) As InvocationExpressionSyntax
            Return SyntaxFactory.InvocationExpression(Nothing, argumentList)
        End Function
    End Class
End Namespace
