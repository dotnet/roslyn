' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UseAutoProperty
    ' https://github.com/dotnet/roslyn/issues/5408
    <Export>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseAutoPropertyAnalyzer
        Inherits AbstractUseAutoPropertyAnalyzer(Of PropertyBlockSyntax, FieldDeclarationSyntax, ModifiedIdentifierSyntax, ExpressionSyntax)

        Private ReadOnly semanticFacts As New VisualBasicSemanticFactsService()

        Protected Overrides Function SupportsReadOnlyProperties(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14
        End Function

        Protected Overrides Function SupportsPropertyInitializer(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic10
        End Function

        Protected Overrides Function GetFieldInitializer(variable As ModifiedIdentifierSyntax, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim declarator = TryCast(variable.Parent, VariableDeclaratorSyntax)
            Return declarator?.Initializer?.Value
        End Function

        Private Function GetFieldName(expression As ExpressionSyntax) As String
            If expression?.Kind() = SyntaxKind.SimpleMemberAccessExpression Then
                Dim memberAccessExpression = DirectCast(expression, MemberAccessExpressionSyntax)
                If memberAccessExpression.Expression.Kind() = SyntaxKind.MeExpression AndAlso
                   memberAccessExpression.Name.Kind() = SyntaxKind.IdentifierName Then
                    Return DirectCast(memberAccessExpression.Name, IdentifierNameSyntax).Identifier.ValueText
                End If
            ElseIf expression.Kind() = SyntaxKind.IdentifierName
                Return DirectCast(expression, IdentifierNameSyntax).Identifier.ValueText
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetGetterFieldName(getMethod As IMethodSymbol, cancellationToken As CancellationToken) As String
            ' Getter has to be of the form:
            '
            '     Get
            '         Return field
            '     End Get
            ' or
            '     Get
            '         Return Me.field
            '     End Get
            Dim accessor = TryCast(TryCast(getMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
            Dim statements = accessor?.Statements
            If statements?.Count = 1 Then
                Dim statement = statements.Value(0)
                If statement.Kind() = SyntaxKind.ReturnStatement Then
                    Dim expr = DirectCast(statement, ReturnStatementSyntax).Expression
                    Return GetFieldName(expr)
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetSetterFieldName(setMethod As IMethodSymbol, cancellationToken As CancellationToken) As String
            ' Setter has to be of the form:
            '
            '     Set(p)
            '         field = p
            '     End Set
            ' or
            '     Set(p)
            '         Me.field = p
            '     End Set
            ' or
            '     Set
            '         field = value
            '     End Set
            ' or
            '     Set
            '         Me.field = value
            '     End Set
            Dim setAccessor = TryCast(TryCast(setMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
            Dim statements = setAccessor?.Statements
            If statements?.Count = 1 Then
                Dim statement = statements.Value(0)
                If statement?.Kind() = SyntaxKind.SimpleAssignmentStatement Then
                    Dim assignmentStatement = DirectCast(statement, AssignmentStatementSyntax)
                    If assignmentStatement.Right.Kind() = SyntaxKind.IdentifierName Then
                        ' Needs to be a something that could be a field of this type on the left.
                        Dim fieldName = GetFieldName(assignmentStatement.Left)
                        If fieldName Is Nothing Then
                            Return Nothing
                        End If

                        ' The right side has to refer to the setter parameter (or 'value' if tehre are no parameters).
                        Dim rightName = DirectCast(assignmentStatement.Right, IdentifierNameSyntax).Identifier.ValueText
                        Dim setParameterList = setAccessor.GetParameterList()
                        If setParameterList IsNot Nothing AndAlso setParameterList.Parameters.Count > 0 Then
                            Return If(CaseInsensitiveComparison.Equals(setParameterList.Parameters(0).Identifier.Identifier.ValueText, rightName), fieldName, Nothing)
                        End If

                        Return If(CaseInsensitiveComparison.Equals(rightName, "value"), fieldName, Nothing)
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
