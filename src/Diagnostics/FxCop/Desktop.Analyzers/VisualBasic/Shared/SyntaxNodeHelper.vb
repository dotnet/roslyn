' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Desktop.Analyzers.Common

    Public NotInheritable Class BasicSyntaxNodeHelper
        Inherits SyntaxNodeHelper

        Shared s_defaultInstance As BasicSyntaxNodeHelper = New BasicSyntaxNodeHelper()

        Public Shared ReadOnly Property DefaultInstance As BasicSyntaxNodeHelper
            Get
                Return s_defaultInstance
            End Get
        End Property

        Private Sub New()
        End Sub

        Public Overrides Function ContainsMethodCall(node As SyntaxNode, predicate As Func(Of String, Boolean)) As Boolean
            If (node Is Nothing) Then
                Return False
            End If

            Return node.DescendantNodesAndSelf().OfType(Of InvocationExpressionSyntax)().Any(
                Function(child As InvocationExpressionSyntax)
                    Return child.DescendantNodesAndSelf().OfType(Of IdentifierNameSyntax)().Any(Function(name) predicate(name.Identifier.ValueText))
                End Function)
        End Function

        Public Overrides Function GetCallerMethodSymbol(node As SyntaxNode, semanticModel As SemanticModel) As IMethodSymbol
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim declaration As MethodBlockSyntax = node.AncestorsAndSelf().OfType(Of MethodBlockSyntax)().FirstOrDefault()
            If (declaration IsNot Nothing) Then
                Return semanticModel.GetDeclaredSymbol(declaration)
            End If

            Dim constructor As ConstructorBlockSyntax = node.AncestorsAndSelf().OfType(Of ConstructorBlockSyntax)().FirstOrDefault()
            If (constructor IsNot Nothing) Then
                Return semanticModel.GetDeclaredSymbol(constructor)
            End If

            Return Nothing
        End Function

        Public Overrides Function GetEnclosingTypeSymbol(node As SyntaxNode, semanticModel As SemanticModel) As ITypeSymbol
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim declaration As ModuleBlockSyntax = node.AncestorsAndSelf().OfType(Of ModuleBlockSyntax)().FirstOrDefault()
            If (declaration Is Nothing) Then
                Return Nothing
            End If

            Return semanticModel.GetDeclaredSymbol(declaration)
        End Function


        Public Overrides Function GetClassDeclarationTypeSymbol(node As SyntaxNode, semanticModel As SemanticModel) As ITypeSymbol
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.ClassBlock) Then
                Return semanticModel.GetDeclaredSymbol(CType(node, ClassBlockSyntax))
            End If

            Return Nothing
        End Function

        Public Overrides Function GetAssignmentLeftNode(node As SyntaxNode) As SyntaxNode
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.SimpleAssignmentStatement) Then
                Return (CType(node, AssignmentStatementSyntax)).Left
            End If

            If (kind = SyntaxKind.VariableDeclarator) Then
                Return (CType(node, VariableDeclaratorSyntax)).Names.First()
            End If

            If (kind = SyntaxKind.NamedFieldInitializer) Then
                Return (CType(node, NamedFieldInitializerSyntax)).Name
            End If

            Return Nothing
        End Function

        Public Overrides Function GetAssignmentRightNode(node As SyntaxNode) As SyntaxNode
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.SimpleAssignmentStatement) Then
                Return (CType(node, AssignmentStatementSyntax)).Right
            End If

            If (kind = SyntaxKind.VariableDeclarator) Then
                Dim decl As VariableDeclaratorSyntax = CType(node, VariableDeclaratorSyntax)

                If (decl.Initializer IsNot Nothing) Then
                    Return decl.Initializer.Value
                End If

                If (decl.AsClause IsNot Nothing) Then
                    Return decl.AsClause
                End If
            End If

            If (kind = SyntaxKind.NamedFieldInitializer) Then
                Return (CType(node, NamedFieldInitializerSyntax)).Expression
            End If

            Return Nothing
        End Function

        Public Overrides Function GetMemberAccessExpressionNode(node As SyntaxNode) As SyntaxNode
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.SimpleMemberAccessExpression) Then
                Return (CType(node, MemberAccessExpressionSyntax)).Expression
            End If

            Return Nothing
        End Function

        Public Overrides Function GetMemberAccessNameNode(node As SyntaxNode) As SyntaxNode
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.SimpleMemberAccessExpression) Then
                Return (CType(node, MemberAccessExpressionSyntax)).Name
            End If

            Return Nothing
        End Function

        Public Overrides Function GetInvocationExpressionNode(node As SyntaxNode) As SyntaxNode
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.InvocationExpression) Then
                Return (CType(node, InvocationExpressionSyntax)).Expression
            End If

            Return Nothing
        End Function

        Public Overrides Function GetCallTargetNode(node As SyntaxNode) As SyntaxNode
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind = SyntaxKind.InvocationExpression) Then
                Dim callExpr As ExpressionSyntax = CType(node, InvocationExpressionSyntax).Expression
                Dim nameNode As SyntaxNode = GetMemberAccessNameNode(callExpr)
                If (nameNode IsNot Nothing) Then
                    Return nameNode
                Else
                    Return callExpr
                End If
            ElseIf (kind = SyntaxKind.ObjectCreationExpression) Then
                Return (CType(node, ObjectCreationExpressionSyntax)).Type
            End If

            Return Nothing
        End Function


        Public Overrides Function GetDefaultValueForAnOptionalParameter(declNode As SyntaxNode, paramIndex As Integer) As SyntaxNode
            If (declNode Is Nothing) Then
                Return Nothing
            End If

            Dim methodDecl As MethodBlockBaseSyntax = CType(declNode, MethodBlockBaseSyntax)
            If (methodDecl Is Nothing) Then
                Return Nothing
            End If

            Dim paramList As ParameterListSyntax = methodDecl.BlockStatement.ParameterList
            If (paramIndex < paramList.Parameters.Count) Then
                Dim equalsValueNode As EqualsValueSyntax = paramList.Parameters(paramIndex).Default
                If (equalsValueNode IsNot Nothing) Then
                    Return equalsValueNode.Value
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetCallArgumentExpressionNodes(node As SyntaxNode, callKind As CallKind) As IEnumerable(Of SyntaxNode)
            If (node Is Nothing) Then
                Return Nothing
            End If

            Dim argList As ArgumentListSyntax = Nothing
            Dim kind As SyntaxKind = node.Kind()

            If (kind = SyntaxKind.InvocationExpression AndAlso ((callKind And CallKind.ObjectCreation) <> 0)) Then
                argList = CType(node, InvocationExpressionSyntax).ArgumentList
            ElseIf ((kind = SyntaxKind.ObjectCreationExpression) AndAlso ((callKind And CallKind.ObjectCreation) <> 0))
                argList = CType(node, ObjectCreationExpressionSyntax).ArgumentList
            End If

            If (argList IsNot Nothing) Then
                Return From arg In argList.Arguments
                       Select arg.GetExpression()
            End If

            Return Enumerable.Empty(Of SyntaxNode)
        End Function

        Public Overrides Function GetObjectInitializerExpressionNodes(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim empty As IEnumerable(Of SyntaxNode) = Enumerable.Empty(Of SyntaxNode)

            If (node Is Nothing) Then
                Return empty
            End If

            Dim kind As SyntaxKind = node.Kind()
            If (kind <> SyntaxKind.ObjectCreationExpression) Then
                Return empty
            End If

            Dim objectCreationNode As ObjectCreationExpressionSyntax = CType(node, ObjectCreationExpressionSyntax)
            If (objectCreationNode.Initializer Is Nothing) Then
                Return empty
            End If

            kind = objectCreationNode.Initializer.Kind()
            If (kind <> SyntaxKind.ObjectMemberInitializer) Then
                Return empty
            End If

            Dim initializer As ObjectMemberInitializerSyntax = CType(objectCreationNode.Initializer, ObjectMemberInitializerSyntax)
            Return From fieldInitializer In initializer.Initializers
                   Where fieldInitializer.Kind() = SyntaxKind.NamedFieldInitializer
                   Select CType(fieldInitializer, NamedFieldInitializerSyntax)
        End Function

        Public Overrides Function IsMethodInvocationNode(node As SyntaxNode) As Boolean
            If (node Is Nothing) Then
                Return False
            End If

            Dim kind As SyntaxKind = node.Kind()
            Return kind = SyntaxKind.InvocationExpression OrElse kind = SyntaxKind.ObjectCreationExpression
        End Function

        Public Overrides Function IsObjectCreationExpressionUnderFieldDeclaration(node As SyntaxNode) As Boolean
            If (node Is Nothing) Then
                Return False
            End If

            Dim kind As SyntaxKind = node.Kind()
            Return kind = SyntaxKind.ObjectCreationExpression AndAlso
                node.AncestorsAndSelf().OfType(Of FieldDeclarationSyntax)().FirstOrDefault() IsNot Nothing
        End Function

        Public Overrides Function GetVariableDeclaratorOfAFieldDeclarationNode(node As SyntaxNode) As SyntaxNode
            If (Not IsObjectCreationExpressionUnderFieldDeclaration(node)) Then
                Return Nothing
            End If

            Return node.AncestorsAndSelf().OfType(Of VariableDeclaratorSyntax)().FirstOrDefault()
        End Function

        Public Overrides Function GetDescendantAssignmentExpressionNodes(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim empty As IEnumerable(Of SyntaxNode) = Enumerable.Empty(Of SyntaxNode)

            If (node Is Nothing) Then
                Return empty
            End If

            Return node.DescendantNodesAndSelf().OfType(Of AssignmentStatementSyntax)()
        End Function

        Public Overrides Function GetDescendantMemberAccessExpressionNodes(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim empty As IEnumerable(Of SyntaxNode) = Enumerable.Empty(Of SyntaxNode)

            If (node Is Nothing) Then
                Return empty
            End If

            Return node.DescendantNodesAndSelf().OfType(Of MemberAccessExpressionSyntax)()
        End Function

    End Class
End Namespace
