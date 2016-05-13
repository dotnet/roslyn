Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.SyntaxTriviaList
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
Partial Friend Class ConvertToInterpolatedStringRefactoringProvider
    Inherits AbstractConvertToInterpolatedStringRefactoringProvider(Of InterpolatedStringExpressionSyntax, InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax)

    Protected Overrides Function GetArguments(invocation As InvocationExpressionSyntax) As SeparatedSyntaxList(Of ArgumentSyntax) ?
        Return invocation?.ArgumentList?.Arguments
    End Function

    Protected Overrides Function GetExpandedArguments(semanticModel As SemanticModel, arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As ImmutableArray(Of ExpressionSyntax)
        Dim builder = ImmutableArray.CreateBuilder(Of ExpressionSyntax)
        For index = 1 To arguments.Count - 1
            builder.Add(CastAndParenthesize(arguments(index).GetExpression, semanticModel))
        Next

        Return builder.ToImmutable()
    End Function

    Protected Overrides Function GetFirstArgument(arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As LiteralExpressionSyntax
        Return TryCast(arguments(0)?.GetExpression(), LiteralExpressionSyntax)
    End Function

    Protected Overrides Function GetFormattingRules(document As Document) As IEnumerable(Of IFormattingRule)
        ' VB does not have multiline comments so we don't need to format them
        Return Nothing
    End Function

    Protected Overrides Function GetInterpolatedString(text As String) As InterpolatedStringExpressionSyntax
        Return CType(ParseExpression("$" + text), InterpolatedStringExpressionSyntax)
    End Function

    Protected Overrides Function GetText(arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As String
        Dim text = CType(arguments(0).GetExpression, LiteralExpressionSyntax).Token.ToString
        ' We need to escape braces as this is an ambiguous case in interpolated strings that is 
        ' not ambiguous in verbatim strings
        Return text.Replace("'{'", "'{{'").Replace("'}'", "'}}'")
    End Function

    Protected Overrides Function IsArgumentListCorrect(
                                                      invocation As InvocationExpressionSyntax,
                                                      invocationSymbol As ISymbol,
                                                      formatMethods As ImmutableArray(Of ISymbol),
                                                      semanticModel As SemanticModel,
                                                      cancellationToken As CancellationToken) As Boolean
        If (invocation.ArgumentList IsNot Nothing AndAlso
            invocation.ArgumentList.Arguments.Count >= 2 AndAlso
            invocation.ArgumentList.Arguments(0).GetExpression().IsKind(SyntaxKind.StringLiteralExpression)) Then
            ' We do not want to substitute the expression if it is being passed to params array argument
            ' Example: 
            ' Dim args as String()
            ' String.Format("{0}{1}{2}", args)
            Return IsArgumentListNotPassingArrayToParams(invocation.ArgumentList.Arguments(1).GetExpression,
                                                         invocationSymbol,
                                                         formatMethods,
                                                         semanticModel,
                                                         cancellationToken)
        End If

        Return False
    End Function

    Protected Overrides Function IsStringLiteral(firstArgument As LiteralExpressionSyntax) As Boolean
        Return If(firstArgument Is Nothing, False, firstArgument.Token.IsKind(SyntaxKind.StringLiteralToken))
    End Function

    Protected Overrides Function VisitArguments(expandedArguments As ImmutableArray(Of ExpressionSyntax), interpolatedString As InterpolatedStringExpressionSyntax) As InterpolatedStringExpressionSyntax
        Return InterpolatedStringRewriter.Visit(interpolatedString, expandedArguments)
    End Function

    Private Shared Function CastAndParenthesize(expression As ExpressionSyntax, semanticModel As SemanticModel) As ExpressionSyntax
        Dim targetType = semanticModel.GetTypeInfo(expression).ConvertedType
        Return Parenthesize(Cast(expression, targetType))
    End Function

    Private Shared Function Cast(expression As ExpressionSyntax, targetType As ITypeSymbol) As ExpressionSyntax
        If targetType Is Nothing Then
            Return expression
        End If

        Dim type = ParseTypeName(targetType.ToDisplayString)
        Return CTypeExpression(Parenthesize(expression), type).WithAdditionalAnnotations(Simplifier.Annotation)
    End Function

    Private Shared Function Parenthesize(expression As ExpressionSyntax) As ExpressionSyntax
        Return If(expression.IsKind(SyntaxKind.ParenthesizedExpression),
                expression,
                ParenthesizedExpression(
                    openParenToken:=Token(Empty, SyntaxKind.OpenParenToken, Empty),
                    expression:=expression,
                    closeParenToken:=Token(Empty, SyntaxKind.CloseParenToken, Empty)).
                    WithAdditionalAnnotations(Simplifier.Annotation))
    End Function
End Class
