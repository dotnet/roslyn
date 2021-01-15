' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SimplifyLinqExpression
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyLinqExpression
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSimplifyLinqExpressionCodeFixProvider
        Inherits AbstractSimplifyLinqExpressionCodeFixProvider(Of
            InvocationExpressionSyntax, SimpleNameSyntax, ExpressionSyntax, ArgumentListSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides Function GetArguments(argumentList As ArgumentListSyntax) As SeparatedSyntaxList(Of SyntaxNode)
            Return argumentList.Arguments
        End Function

        Protected Overrides Function GetName(invocationExpression As InvocationExpressionSyntax) As SimpleNameSyntax
            Return invocationExpression.Expression.GetRightmostName()
        End Function

    End Class
End Namespace
