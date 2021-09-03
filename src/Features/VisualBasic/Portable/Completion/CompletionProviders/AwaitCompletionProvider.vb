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
Imports System.Threading
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery

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

        Private Protected Overrides Function GetTypeSymbolOfExpression(semanticModel As SemanticModel, potentialAwaitableExpression As SyntaxNode, cancellationToken As CancellationToken) As ITypeSymbol
            Dim memberAccessExpression = TryCast(potentialAwaitableExpression, MemberAccessExpressionSyntax)?.Expression
            If memberAccessExpression IsNot Nothing Then
                Dim symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpression.WalkDownParentheses(), cancellationToken)
                Dim symbol = symbolInfo.Symbol
                If TypeOf symbol Is ITypeSymbol Then ' e.g. Task.$$
                    Return Nothing
                End If

                Return If(symbol?.GetSymbolType(),
                    If(symbol?.GetMemberType(),
                    semanticModel.GetTypeInfo(memberAccessExpression, cancellationToken).Type))
            End If

            Return Nothing
        End Function

        Private Protected Overrides Function GetExpressionToPlaceAwaitInFrontOf(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxNode
            Dim dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken)
            If Not dotToken.HasValue Then
                Return Nothing
            End If

            If TypeOf dotToken.Value.Parent Is MemberAccessExpressionSyntax Then
                Dim memberAccess = CType(dotToken.Value.Parent, MemberAccessExpressionSyntax)
                If memberAccess.Expression.GetParentConditionalAccessExpression() Is Nothing Then
                    Return memberAccess
                End If
            End If

            Return Nothing
        End Function

        Private Protected Overrides Function GetDotTokenLeftOfPosition(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxToken?
            Dim tokenOnLeft = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Dim dotToken = tokenOnLeft.GetPreviousTokenIfTouchingWord(position)
            If Not dotToken.IsKind(SyntaxKind.DotToken) Then
                Return Nothing
            End If

            If dotToken.GetPreviousToken().IsKind(SyntaxKind.IntegerLiteralToken, SyntaxKind.FloatingLiteralToken, SyntaxKind.DecimalLiteralToken, SyntaxKind.DateLiteralToken) Then
                Return Nothing
            End If

            Return dotToken
        End Function
    End Class
End Namespace
