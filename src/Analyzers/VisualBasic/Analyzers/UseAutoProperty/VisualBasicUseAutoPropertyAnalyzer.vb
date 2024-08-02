' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseAutoProperty
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseAutoPropertyAnalyzer
        Inherits AbstractUseAutoPropertyAnalyzer(Of
            SyntaxKind,
            PropertyBlockSyntax,
            ConstructorBlockSyntax,
            FieldDeclarationSyntax,
            ModifiedIdentifierSyntax,
            ExpressionSyntax,
            IdentifierNameSyntax)

        Protected Overrides ReadOnly Property PropertyDeclarationKind As SyntaxKind = SyntaxKind.PropertyBlock

        Protected Overrides ReadOnly Property SemanticFacts As ISemanticFacts = VisualBasicSemanticFacts.Instance

        Protected Overrides Function SupportsReadOnlyProperties(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14
        End Function

        Protected Overrides Function SupportsPropertyInitializer(compilation As Compilation) As Boolean
            Return DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic10
        End Function

        Protected Overrides Function SupportsFieldExpression(compilation As Compilation) As Boolean
            ' 'field' keyword not supported in VB.
            Return False
        End Function

        Protected Overrides ReadOnly Property CanExplicitInterfaceImplementationsBeFixed As Boolean = False
        Protected Overrides ReadOnly Property SupportsFieldAttributesOnProperties As Boolean = False

        Protected Overrides Function ContainsFieldExpression(propertyDeclaration As PropertyBlockSyntax, cancellationToken As CancellationToken) As Boolean
            Return False
        End Function

        Protected Overrides Sub RegisterIneligibleFieldsAction(
                fieldNames As HashSet(Of String),
                ineligibleFields As ConcurrentDictionary(Of IFieldSymbol, ConcurrentSet(Of SyntaxNode)),
                semanticModel As SemanticModel,
                codeBlock As SyntaxNode,
                cancellationToken As CancellationToken)
            ' There are no syntactic constructs that make a field ineligible to be replaced with 
            ' a property.  In C# you can't use a property in a ref/out position.  But that restriction
            ' doesn't apply to VB.
        End Sub

        Protected Overrides Function CanConvert(prop As IPropertySymbol) As Boolean
            ' VB auto props cannot specify accessibility for accessors.  So if the original
            ' code had different accessibility between the accessors and property then we 
            ' can't convert it.

            If prop.GetMethod IsNot Nothing AndAlso
               prop.GetMethod.DeclaredAccessibility <> prop.DeclaredAccessibility Then
                Return False
            End If

            If prop.SetMethod IsNot Nothing AndAlso
               prop.SetMethod.DeclaredAccessibility <> prop.DeclaredAccessibility Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function GetFieldInitializer(variable As ModifiedIdentifierSyntax, cancellationToken As CancellationToken) As ExpressionSyntax
            Dim declarator = TryCast(variable.Parent, VariableDeclaratorSyntax)
            Return declarator?.Initializer?.Value
        End Function

        Private Shared Function CheckExpressionSyntactically(expression As ExpressionSyntax) As Boolean
            If expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) Then
                Dim memberAccessExpression = DirectCast(expression, MemberAccessExpressionSyntax)
                Return memberAccessExpression.Expression.Kind() = SyntaxKind.MeExpression AndAlso
                    memberAccessExpression.Name.Kind() = SyntaxKind.IdentifierName
            ElseIf expression.IsKind(SyntaxKind.IdentifierName) Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetGetterExpression(getMethod As IMethodSymbol, cancellationToken As CancellationToken) As ExpressionSyntax
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
                    Return If(CheckExpressionSyntactically(expr), expr, Nothing)
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetSetterExpression(semanticModel As SemanticModel, setMethod As IMethodSymbol, cancellationToken As CancellationToken) As ExpressionSyntax
            ' Setter has to be of the form:
            '
            '     Set(value)
            '         field = value
            '     End Set
            ' or
            '     Set(value)
            '         Me.field = value
            '     End Set
            Dim setAccessor = TryCast(TryCast(setMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax)?.Parent, AccessorBlockSyntax)
            Dim statements = setAccessor?.Statements
            If statements?.Count = 1 Then
                Dim statement = statements.Value(0)
                If statement.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                    Dim assignmentStatement = DirectCast(statement, AssignmentStatementSyntax)
                    If assignmentStatement.Right.Kind() = SyntaxKind.IdentifierName Then
                        Dim identifier = DirectCast(assignmentStatement.Right, IdentifierNameSyntax)
                        Dim symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol
                        If setMethod.Parameters.Contains(TryCast(symbol, IParameterSymbol)) Then
                            Return If(CheckExpressionSyntactically(assignmentStatement.Left), assignmentStatement.Left, Nothing)
                        End If
                    End If
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetFieldNode(fieldDeclaration As FieldDeclarationSyntax, identifier As ModifiedIdentifierSyntax) As SyntaxNode
            Return GetNodeToRemove(identifier)
        End Function

        Protected Overrides Sub AddAccessedFields(semanticModel As SemanticModel, accessor As IMethodSymbol, fieldNames As HashSet(Of String), result As HashSet(Of IFieldSymbol), cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable()
        End Sub
    End Class
End Namespace
