' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGeneratorInternal), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxGeneratorInternal
        Inherits SyntaxGeneratorInternal

        Public Shared ReadOnly Instance As SyntaxGeneratorInternal = New VisualBasicSyntaxGeneratorInternal()

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Friend Overloads Overrides Function LocalDeclarationStatement(type As SyntaxNode, identifier As SyntaxToken, Optional initializer As SyntaxNode = Nothing, Optional isConst As Boolean = False) As SyntaxNode
            Return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(If(isConst, SyntaxKind.ConstKeyword, SyntaxKind.DimKeyword))),
                SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, SyntaxFactory.ModifiedIdentifier(identifier), initializer)))
        End Function

        Friend Overrides Function WithInitializer(variableDeclarator As SyntaxNode, initializer As SyntaxNode) As SyntaxNode
            Return DirectCast(variableDeclarator, VariableDeclaratorSyntax).WithInitializer(DirectCast(initializer, EqualsValueSyntax))
        End Function

        Friend Overrides Function EqualsValueClause(operatorToken As SyntaxToken, value As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.EqualsValue(operatorToken, DirectCast(value, ExpressionSyntax))
        End Function

        Friend Shared Function VariableDeclarator(type As SyntaxNode, name As ModifiedIdentifierSyntax, Optional expression As SyntaxNode = Nothing) As VariableDeclaratorSyntax
            Return SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(name),
                If(type Is Nothing, Nothing, SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))),
                If(expression Is Nothing,
                   Nothing,
                   SyntaxFactory.EqualsValue(DirectCast(expression, ExpressionSyntax))))
        End Function

        Friend Overrides Function Identifier(text As String) As SyntaxToken
            Return SyntaxFactory.Identifier(text)
        End Function

        Friend Overrides Function ConditionalAccessExpression(expression As SyntaxNode, whenNotNull As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ConditionalAccessExpression(
                DirectCast(expression, ExpressionSyntax),
                DirectCast(whenNotNull, ExpressionSyntax))
        End Function

        Friend Overrides Function MemberBindingExpression(name As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SimpleMemberAccessExpression(DirectCast(name, SimpleNameSyntax))
        End Function

        Friend Overrides Function RefExpression(expression As SyntaxNode) As SyntaxNode
            Return expression
        End Function

        Friend Overrides Function AddParentheses(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True, Optional addSimplifierAnnotation As Boolean = True) As SyntaxNode
            Return Parenthesize(expression, addSimplifierAnnotation)
        End Function

        Friend Shared Function Parenthesize(expression As SyntaxNode, Optional addSimplifierAnnotation As Boolean = True) As ParenthesizedExpressionSyntax
            Return DirectCast(expression, ExpressionSyntax).Parenthesize(addSimplifierAnnotation)
        End Function

        Friend Overrides Function YieldReturnStatement(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.YieldStatement(DirectCast(expression, ExpressionSyntax))
        End Function

        Friend Overrides Function RequiresLocalDeclarationType() As Boolean
            ' VB supports `dim x = ...` as well as `dim x as Y = ...`.  The local declaration type
            ' is not required.
            Return False
        End Function

        Friend Overrides Function InterpolatedStringExpression(startToken As SyntaxToken, content As IEnumerable(Of SyntaxNode), endToken As SyntaxToken) As SyntaxNode
            Return SyntaxFactory.InterpolatedStringExpression(
                startToken, SyntaxFactory.List(content.Cast(Of InterpolatedStringContentSyntax)), endToken)
        End Function

        Friend Overrides Function InterpolatedStringText(textToken As SyntaxToken) As SyntaxNode
            Return SyntaxFactory.InterpolatedStringText(textToken)
        End Function

        Friend Overrides Function InterpolatedStringTextToken(content As String) As SyntaxToken
            Return SyntaxFactory.InterpolatedStringTextToken(content, "")
        End Function

        Friend Overrides Function Interpolation(syntaxNode As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.Interpolation(DirectCast(syntaxNode, ExpressionSyntax))
        End Function

        Friend Overrides Function InterpolationAlignmentClause(alignment As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.InterpolationAlignmentClause(
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                DirectCast(alignment, ExpressionSyntax))
        End Function

        Friend Overrides Function InterpolationFormatClause(format As String) As SyntaxNode
            Return SyntaxFactory.InterpolationFormatClause(
                SyntaxFactory.Token(SyntaxKind.ColonToken),
                SyntaxFactory.InterpolatedStringTextToken(format, format))
        End Function

        Friend Overrides Function TypeParameterList(typeParameterNames As IEnumerable(Of String)) As SyntaxNode
            Return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(Of TypeParameterSyntax)(
                    typeParameterNames.Select(Function(n) SyntaxFactory.TypeParameter(n))))
        End Function

#Region "Patterns"

        Friend Overrides Function SupportsPatterns(options As ParseOptions) As Boolean
            Return False
        End Function

        Friend Overrides Function IsPatternExpression(expression As SyntaxNode, isToken As SyntaxToken, pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function ConstantPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function DeclarationPattern(type As INamedTypeSymbol, name As String) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function AndPattern(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function NotPattern(pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function OrPattern(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function ParenthesizedPattern(pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function TypePattern(type As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

#End Region
    End Class
End Namespace
