' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(AwaitCompletionProvider), LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=NameOf(KeywordCompletionProvider))>
    Friend NotInheritable Class AwaitCompletionProvider
        Inherits AbstractAwaitCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
        End Sub

        Friend Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CommonTriggerChars

        Protected Overrides Function GetAsyncKeywordInsertionPosition(declaration As SyntaxNode) As Integer
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

        Protected Overrides Function GetReturnTypeChangeAsync(solution As Solution, semanticModel As SemanticModel, declaration As SyntaxNode, cancellationToken As CancellationToken) As Task(Of TextChange?)
            ' Todo: Add support if desired.
            Return SpecializedTasks.Default(Of TextChange?)
        End Function

        Protected Overrides Function GetAsyncSupportingDeclaration(targetToken As SyntaxToken, position As Integer) As SyntaxNode
            Return targetToken.GetAncestor(Function(node) node.IsAsyncSupportedFunctionSyntax())
        End Function

        Protected Overrides Function GetTypeSymbolOfExpression(semanticModel As SemanticModel, potentialAwaitableExpression As SyntaxNode, cancellationToken As CancellationToken) As ITypeSymbol
            Dim memberAccessExpression = TryCast(potentialAwaitableExpression, MemberAccessExpressionSyntax)?.Expression
            If memberAccessExpression Is Nothing Then
                Return Nothing
            End If

            Dim symbol = semanticModel.GetSymbolInfo(memberAccessExpression.WalkDownParentheses(), cancellationToken).Symbol
            Return If(TypeOf symbol Is ITypeSymbol, Nothing, semanticModel.GetTypeInfo(memberAccessExpression, cancellationToken).Type)
        End Function

        Protected Overrides Function GetExpressionToPlaceAwaitInFrontOf(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxNode
            Dim dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken)
            If Not dotToken.HasValue Then
                Return Nothing
            End If

            Dim memberAccess = TryCast(dotToken.Value.Parent, MemberAccessExpressionSyntax)
            If memberAccess Is Nothing Then
                Return Nothing
            End If

            If memberAccess.Expression.GetParentConditionalAccessExpression() IsNot Nothing Then
                Return Nothing
            End If

            Return memberAccess
        End Function

        Protected Overrides Function GetDotTokenLeftOfPosition(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxToken?
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
