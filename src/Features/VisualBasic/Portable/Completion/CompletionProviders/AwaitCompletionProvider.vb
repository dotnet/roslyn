' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(AwaitCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(KeywordCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class AwaitCompletionProvider
        Inherits AbstractAwaitCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CommonTriggerChars

        Private Protected Overrides Function GetSpanStart(declaration As SyntaxNode) As Integer
            Select Case declaration.Kind()
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, DeclarationStatementSyntax).GetMemberKeywordToken().SpanStart
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declaration, LambdaExpressionSyntax).SubOrFunctionHeader.SubOrFunctionKeyword.SpanStart
            End Select

            Throw ExceptionUtilities.Unreachable
        End Function

        Private Protected Overrides Function GetAsyncSupportingDeclaration(token As SyntaxToken) As SyntaxNode
            Return token.GetAncestor(Function(node) node.IsAsyncSupportedFunctionSyntax())
        End Function
    End Class
End Namespace
