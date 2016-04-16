' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportLanguageService(GetType(IIntroduceVariableService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicIntroduceVariableService
        Inherits AbstractIntroduceVariableService(Of VisualBasicIntroduceVariableService, ExpressionSyntax, TypeSyntax, TypeBlockSyntax, QueryExpressionSyntax)

        Protected Overrides Function GetContainingExecutableBlocks(expression As ExpressionSyntax) As IEnumerable(Of SyntaxNode)
            Return expression.GetContainingExecutableBlocks()
        End Function

        Protected Overrides Function GetInsertionIndices(destination As TypeBlockSyntax, cancellationToken As CancellationToken) As IList(Of Boolean)
            Return destination.GetInsertionIndices(cancellationToken)
        End Function

        Protected Overrides Function IsInAttributeArgumentInitializer(expression As ExpressionSyntax) As Boolean
            If expression.GetAncestorOrThis(Of ArgumentSyntax)() Is Nothing Then
                Return False
            End If

            If expression.GetAncestorOrThis(Of AttributeSyntax)() Is Nothing Then
                Return False
            End If

            If expression.DepthFirstTraversal.Any(Function(n) n.Kind() = SyntaxKind.ArrayCreationExpression) OrElse
               expression.DepthFirstTraversal.Any(Function(n) n.Kind() = SyntaxKind.GetTypeExpression) Then
                Return False
            End If

            Dim attributeBlock = expression.GetAncestorOrThis(Of AttributeListSyntax)()
            If attributeBlock.IsParentKind(SyntaxKind.CompilationUnit) Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function IsInConstructorInitializer(expression As ExpressionSyntax) As Boolean
            Dim constructorInitializer = expression.GetAncestorsOrThis(Of StatementSyntax)().
                  Where(Function(n) n.IsConstructorInitializer()).
                  FirstOrDefault()

            If constructorInitializer Is Nothing Then
                Return False
            End If

            ' have to make sure we're not inside a lambda inside the constructor initializer.
            If expression.GetAncestorOrThis(Of LambdaExpressionSyntax)() IsNot Nothing Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function CanIntroduceVariableFor(expression As ExpressionSyntax) As Boolean
            expression = expression.WalkUpParentheses()

            If TypeOf expression.Parent Is CallStatementSyntax Then
                Return False
            End If

            If Not expression.GetImplicitMemberAccessExpressions.All(Function(e) e.IsParentKind(SyntaxKind.WithStatement)) Then
                Return False
            End If

            If expression.IsParentKind(SyntaxKind.EqualsValue) AndAlso
               expression.Parent.IsParentKind(SyntaxKind.VariableDeclarator) Then
                Return False
            End If

            ' For Nothing Literals, AllOccurrences could introduce semantic errors.
            If expression.IsKind(SyntaxKind.NothingLiteralExpression) Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function IsInFieldInitializer(expression As ExpressionSyntax) As Boolean
            If expression.GetAncestorOrThis(Of VariableDeclaratorSyntax)().GetAncestorOrThis(Of FieldDeclarationSyntax)() IsNot Nothing Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function IsInNonFirstQueryClause(expression As ExpressionSyntax) As Boolean
            Dim query = expression.GetAncestor(Of QueryExpressionSyntax)()
            If query Is Nothing Then
                Return False
            End If

            ' Can't introduce for the first clause in a query.
            Dim fromClause = expression.GetAncestor(Of FromClauseSyntax)()
            If fromClause IsNot Nothing AndAlso query.Clauses.First() Is fromClause Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function IsInParameterInitializer(expression As ExpressionSyntax) As Boolean
            Return expression.GetAncestorOrThis(Of EqualsValueSyntax)().IsParentKind(SyntaxKind.Parameter)
        End Function

        Protected Overrides Function IsInAutoPropertyInitializer(expression As ExpressionSyntax) As Boolean
            Dim propertyStatement = expression.GetAncestorOrThis(Of PropertyStatementSyntax)()
            If propertyStatement IsNot Nothing Then
                Return expression.GetAncestorsOrThis(Of AsClauseSyntax).Contains(propertyStatement.AsClause) OrElse
                    expression.GetAncestorOrThis(Of EqualsValueSyntax).Contains(propertyStatement.Initializer)
            End If

            Return False
        End Function

        Protected Overrides Function IsInExpressionBodiedMember(expression As ExpressionSyntax) As Boolean
            Return False
        End Function

        Protected Overrides Function CanReplace(expression As ExpressionSyntax) As Boolean
            If expression.CheckParent(Of RangeArgumentSyntax)(Function(n) n.LowerBound Is expression) Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function RewriteCore(Of TNode As SyntaxNode)(node As TNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As TNode
            Return DirectCast(Rewriter.Visit(node, replacementNode, matches), TNode)
        End Function

        Protected Overrides Function BlockOverlapsHiddenPosition(block As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Dim statements = block.GetStatements()

            If statements.Count = 0 Then
                Return block.OverlapsHiddenPosition(cancellationToken)
            End If

            Dim first = statements.First()
            Dim last = statements.Last()

            Return block.OverlapsHiddenPosition(TextSpan.FromBounds(first.SpanStart, last.SpanStart), cancellationToken)
        End Function

    End Class
End Namespace
