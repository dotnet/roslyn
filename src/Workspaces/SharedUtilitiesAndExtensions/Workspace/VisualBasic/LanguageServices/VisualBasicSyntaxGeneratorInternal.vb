' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations
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

        Public Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Public Overrides Function EndOfLine(text As String) As SyntaxTrivia
            Return SyntaxFactory.EndOfLine(text)
        End Function

        Public Overloads Overrides Function LocalDeclarationStatement(type As SyntaxNode, identifier As SyntaxToken, Optional initializer As SyntaxNode = Nothing, Optional isConst As Boolean = False) As SyntaxNode
            Return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(If(isConst, SyntaxKind.ConstKeyword, SyntaxKind.DimKeyword))),
                SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, SyntaxFactory.ModifiedIdentifier(identifier), initializer)))
        End Function

        Public Overrides Function WithInitializer(variableDeclarator As SyntaxNode, initializer As SyntaxNode) As SyntaxNode
            Return DirectCast(variableDeclarator, VariableDeclaratorSyntax).WithInitializer(DirectCast(initializer, EqualsValueSyntax))
        End Function

        Public Overrides Function EqualsValueClause(operatorToken As SyntaxToken, value As SyntaxNode) As SyntaxNode
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

        Public Overrides Function Identifier(text As String) As SyntaxToken
            Return SyntaxFactory.Identifier(text)
        End Function

        Public Overrides Function ConditionalAccessExpression(expression As SyntaxNode, whenNotNull As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ConditionalAccessExpression(
                DirectCast(expression, ExpressionSyntax),
                DirectCast(whenNotNull, ExpressionSyntax))
        End Function

        Public Overrides Function MemberBindingExpression(name As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SimpleMemberAccessExpression(DirectCast(name, SimpleNameSyntax))
        End Function

        Public Overrides Function RefExpression(expression As SyntaxNode) As SyntaxNode
            Return expression
        End Function

        Public Overrides Function AddParentheses(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True, Optional addSimplifierAnnotation As Boolean = True) As SyntaxNode
            Return Parenthesize(expression, addSimplifierAnnotation)
        End Function

        Friend Shared Function Parenthesize(expression As SyntaxNode, Optional addSimplifierAnnotation As Boolean = True) As ParenthesizedExpressionSyntax
            Return DirectCast(expression, ExpressionSyntax).Parenthesize(addSimplifierAnnotation)
        End Function

        Public Overrides Function YieldReturnStatement(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.YieldStatement(DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overrides Function RequiresLocalDeclarationType() As Boolean
            ' VB supports `dim x = ...` as well as `dim x as Y = ...`.  The local declaration type
            ' is not required.
            Return False
        End Function

        Public Overrides Function InterpolatedStringExpression(startToken As SyntaxToken, content As IEnumerable(Of SyntaxNode), endToken As SyntaxToken) As SyntaxNode
            Return SyntaxFactory.InterpolatedStringExpression(
                startToken, SyntaxFactory.List(content.Cast(Of InterpolatedStringContentSyntax)), endToken)
        End Function

        Public Overrides Function InterpolatedStringText(textToken As SyntaxToken) As SyntaxNode
            Return SyntaxFactory.InterpolatedStringText(textToken)
        End Function

        Public Overrides Function InterpolatedStringTextToken(content As String, value As String) As SyntaxToken
            Return SyntaxFactory.InterpolatedStringTextToken(content, value)
        End Function

        Public Overrides Function Interpolation(syntaxNode As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.Interpolation(DirectCast(syntaxNode, ExpressionSyntax))
        End Function

        Public Overrides Function InterpolationAlignmentClause(alignment As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.InterpolationAlignmentClause(
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                DirectCast(alignment, ExpressionSyntax))
        End Function

        Public Overrides Function InterpolationFormatClause(format As String) As SyntaxNode
            Return SyntaxFactory.InterpolationFormatClause(
                SyntaxFactory.Token(SyntaxKind.ColonToken),
                SyntaxFactory.InterpolatedStringTextToken(format, format))
        End Function

        Public Overrides Function TypeParameterList(typeParameterNames As IEnumerable(Of String)) As SyntaxNode
            Return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(Of TypeParameterSyntax)(
                    typeParameterNames.Select(Function(n) SyntaxFactory.TypeParameter(n))))
        End Function

        Public Overrides Function Type(typeSymbol As ITypeSymbol, typeContext As Boolean) As SyntaxNode
            Return If(typeContext, typeSymbol.GenerateTypeSyntax(), typeSymbol.GenerateExpressionSyntax())
        End Function

        Public Overrides Function NegateEquality(generator As SyntaxGenerator, node As SyntaxNode, left As SyntaxNode, negatedKind As Operations.BinaryOperatorKind, right As SyntaxNode) As SyntaxNode
            Select Case negatedKind
                Case BinaryOperatorKind.Equals
                    Return If(node.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression),
                        generator.ValueEqualsExpression(left, right),
                        generator.ReferenceEqualsExpression(left, right))
                Case BinaryOperatorKind.NotEquals
                    Return If(node.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression),
                        generator.ValueNotEqualsExpression(left, right),
                        generator.ReferenceNotEqualsExpression(left, right))
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(negatedKind)
            End Select
        End Function

        Public Overrides Function IsNotTypeExpression(expression As SyntaxNode, type As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.TypeOfIsNotExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax))
        End Function

#Region "Patterns"

        Public Overrides Function SupportsPatterns(options As ParseOptions) As Boolean
            Return False
        End Function

        Public Overrides Function IsPatternExpression(expression As SyntaxNode, isToken As SyntaxToken, pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function ConstantPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function DeclarationPattern(type As INamedTypeSymbol, name As String) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function AndPattern(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function NotPattern(pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function OrPattern(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function ParenthesizedPattern(pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function TypePattern(type As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function UnaryPattern(operatorToken As SyntaxToken, pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

#End Region
    End Class
End Namespace
