' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty
    <Export>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class UseAutoPropertyAnalyzer
        Inherits AbstractUseAutoPropertyAnalyzer(Of PropertyBlockSyntax, FieldDeclarationSyntax, ModifiedIdentifierSyntax, ExpressionSyntax)

        Private ReadOnly semanticFacts As New VisualBasicSemanticFactsService()

        Protected Overrides Function SupportsReadOnlyProperties(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14
        End Function

        Protected Overrides Function SupportsPropertyInitializer(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic10
        End Function

        Protected Overrides Sub RegisterIneligibleFieldsAction(context As CompilationStartAnalysisContext, ineligibleFields As ConcurrentBag(Of IFieldSymbol))
            ' There are no syntactic constructs that make a field ineligible to be replaced with 
            ' a property.  In C# you can't use a property in a ref/out positoin.  But that restriction
            ' doesn't apply to VB.
        End Sub

        Protected Overrides Function GetFieldInitializer(variable As ModifiedIdentifierSyntax, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim declarator = TryCast(variable.Parent, VariableDeclaratorSyntax)
            Return declarator?.Initializer?.Value
        End Function

        Private Function CheckExpressionSyntactically(expression As ExpressionSyntax) As Boolean
            If expression?.Kind() = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccessExpression = DirectCast(expression, MemberAccessExpressionSyntax)
                Return memberAccessExpression.Expression.Kind() = SyntaxKind.MeExpression AndAlso
                    memberAccessExpression.Name.Kind() = SyntaxKind.IdentifierName
            ElseIf expression.Kind() = SyntaxKind.IdentifierName
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetGetterExpression(getMethod As IMethodSymbol, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim accessor = TryCast(TryCast(getMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
            Dim statements = accessor?.Statements
            If statements?.Count = 1 Then
                ' this only works with a getter body with exactly one statement
                Dim firstStatement = statements.Value(0)
                If firstStatement.Kind() = SyntaxKind.ReturnStatement Then
                    Dim expr = DirectCast(firstStatement, ReturnStatementSyntax).Expression
                    Return If(CheckExpressionSyntactically(expr), expr, Nothing)
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetSetterExpression(setMethod As IMethodSymbol, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim setAccessor = TryCast(TryCast(setMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)

            Dim firstStatement = setAccessor?.Statements.SingleOrDefault()
            If firstStatement?.Kind() = SyntaxKind.SimpleAssignmentStatement Then
                Dim assignmentStatement = DirectCast(firstStatement, AssignmentStatementSyntax)
                If assignmentStatement.Right.Kind() = SyntaxKind.IdentifierName Then
                    Dim identifier = DirectCast(assignmentStatement.Right, IdentifierNameSyntax)
                    Dim symbol = semanticModel.GetSymbolInfo(identifier).Symbol
                    If setMethod.Parameters.Contains(TryCast(symbol, IParameterSymbol)) Then
                        Return If(CheckExpressionSyntactically(assignmentStatement.Left), assignmentStatement.Left, Nothing)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetNodeToFade(fieldDeclaration As FieldDeclarationSyntax, identifier As ModifiedIdentifierSyntax) As SyntaxNode
            Return Utilities.GetNodeToRemove(identifier)
        End Function

        Protected Overrides Function IsEligibleHeuristic(field As IFieldSymbol, propertyDeclaration As PropertyBlockSyntax, compilation As Compilation, cancellationToken As CancellationToken) As Boolean
            If propertyDeclaration.Accessors.Any(SyntaxKind.SetAccessorBlock) Then
                ' If this property already has a setter, then we can definitely simplify it to an auto-prop 
                Return True
            End If

            ' the property doesn't have a setter currently. check all the types the field is 
            ' declared in.  If the field is written to outside of a constructor, then this 
            ' field Is Not elegible for replacement with an auto prop.  We'd have to make 
            ' the autoprop read/write, And that could be opening up the propert widely 
            ' (in accessibility terms) in a way the user would not want.
            Dim containingType = field.ContainingType
            For Each ref In containingType.DeclaringSyntaxReferences
                Dim containingNode = ref.GetSyntax(cancellationToken)?.Parent
                If containingNode IsNot Nothing Then
                    Dim semanticModel = compilation.GetSemanticModel(containingNode.SyntaxTree)
                    If IsWrittenOutsideOfConstructorOrProperty(field, propertyDeclaration, containingNode, semanticModel, cancellationToken) Then
                        Return False
                    End If
                End If
            Next

            ' No problem simplifying this field.
            Return True
        End Function

        Private Function IsWrittenOutsideOfConstructorOrProperty(field As IFieldSymbol,
                                                                 propertyDeclaration As PropertyBlockSyntax,
                                                                 node As SyntaxNode,
                                                                 semanticModel As SemanticModel,
                                                                 cancellationToken As CancellationToken) As Boolean
            cancellationToken.ThrowIfCancellationRequested()

            If node Is propertyDeclaration Then
                Return False
            End If

            If node.Kind() = SyntaxKind.ConstructorBlock Then
                Return False
            End If

            If node.Kind() = SyntaxKind.IdentifierName Then
                Dim symbolInfo = semanticModel.GetSymbolInfo(node)
                If field.Equals(symbolInfo.Symbol) Then
                    If semanticFacts.IsWrittenTo(semanticModel, node, cancellationToken) Then
                        Return True
                    End If
                End If
            Else
                For Each child In node.ChildNodesAndTokens
                    If child.IsNode Then
                        If IsWrittenOutsideOfConstructorOrProperty(field, propertyDeclaration, child.AsNode(), semanticModel, cancellationToken) Then
                            Return True
                        End If
                    End If
                Next
            End If

            Return False
        End Function
    End Class
End Namespace
