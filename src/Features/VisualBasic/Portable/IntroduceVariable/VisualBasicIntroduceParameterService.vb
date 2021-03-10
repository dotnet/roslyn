' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceParameterService
        Inherits AbstractIntroduceParameterService(Of VisualBasicIntroduceParameterService, ExpressionSyntax, InvocationExpressionSyntax, IdentifierNameSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function AddArgumentToArgumentList(invocationArguments As SeparatedSyntaxList(Of SyntaxNode), newArgumentExpression As SyntaxNode) As SeparatedSyntaxList(Of SyntaxNode)
            Return invocationArguments.Add(SyntaxFactory.SimpleArgument(DirectCast(newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation), ExpressionSyntax)))
        End Function

        Protected Overrides Function AddExpressionArgumentToArgumentList(arguments As ImmutableArray(Of SyntaxNode), expression As SyntaxNode) As ImmutableArray(Of SyntaxNode)
            Dim newArgument = SyntaxFactory.SimpleArgument(DirectCast(expression, ExpressionSyntax))
            Return arguments.Add(newArgument)
        End Function

        Protected Overrides Function IsMethodDeclaration(node As SyntaxNode) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    Return True
            End Select

            Return False
        End Function

        Protected Overrides Function RewriteCore(Of TNode As SyntaxNode)(node As TNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As TNode
            Return DirectCast(Rewriter.Visit(node, replacementNode, matches), TNode)
        End Function
    End Class
End Namespace
