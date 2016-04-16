' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Composition
Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ReplaceMethodWithProperty

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    <ExportLanguageService(GetType(IReplaceMethodWithPropertyService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReplaceMethodWithPropertyService
        Implements IReplaceMethodWithPropertyService

        Public Function GetMethodName(methodNode As SyntaxNode) As String Implements IReplaceMethodWithPropertyService.GetMethodName
            Return DirectCast(methodNode, MethodStatementSyntax).Identifier.ValueText
        End Function

        Public Function GetMethodDeclaration(token As SyntaxToken) As SyntaxNode Implements IReplaceMethodWithPropertyService.GetMethodDeclaration
            Dim containingMethod = token.Parent.FirstAncestorOrSelf(Of MethodStatementSyntax)
            If containingMethod Is Nothing Then
                Return Nothing
            End If

            Dim start = If(containingMethod.AttributeLists.Count > 0,
                containingMethod.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart,
                 containingMethod.SpanStart)

            ' Offer this refactoring anywhere in the signature of the method.
            Dim position = token.SpanStart
            If position < start Then
                Return Nothing
            End If

            If containingMethod.HasReturnType() AndAlso
                position > containingMethod.GetReturnType().Span.End Then
                Return Nothing
            End If

            If position > containingMethod.ParameterList.Span.End Then
                Return Nothing
            End If

            Return containingMethod
        End Function

        Public Sub RemoveSetMethod(editor As SyntaxEditor, setMethodDeclaration As SyntaxNode) Implements IReplaceMethodWithPropertyService.RemoveSetMethod
            Dim setMethodStatement = TryCast(setMethodDeclaration, MethodStatementSyntax)
            If setMethodStatement Is Nothing Then
                Return
            End If

            Dim methodOrBlock = GetParentIfBlock(setMethodStatement)
            editor.RemoveNode(methodOrBlock)
        End Sub

        Public Sub ReplaceGetMethodWithProperty(
            editor As SyntaxEditor,
            semanticModel As SemanticModel,
            getAndSetMethods As GetAndSetMethods,
            propertyName As String, nameChanged As Boolean) Implements IReplaceMethodWithPropertyService.ReplaceGetMethodWithProperty

            Dim getMethodDeclaration = TryCast(getAndSetMethods.GetMethodDeclaration, MethodStatementSyntax)
            If getMethodDeclaration Is Nothing Then
                Return
            End If

            Dim methodBlockOrStatement = GetParentIfBlock(getMethodDeclaration)
            editor.ReplaceNode(methodBlockOrStatement,
                               ConvertMethodsToProperty(editor, semanticModel, getAndSetMethods, propertyName, nameChanged))
        End Sub

        Private Function GetParentIfBlock(declaration As MethodStatementSyntax) As DeclarationStatementSyntax
            If declaration.IsParentKind(SyntaxKind.FunctionBlock) OrElse declaration.IsParentKind(SyntaxKind.SubBlock) Then
                Return DirectCast(declaration.Parent, DeclarationStatementSyntax)
            End If

            Return declaration
        End Function

        Private Function ConvertMethodsToProperty(
            editor As SyntaxEditor,
            semanticModel As SemanticModel,
            getAndSetMethods As GetAndSetMethods,
            propertyName As String, nameChanged As Boolean) As DeclarationStatementSyntax

            Dim generator = editor.Generator

            Dim getMethodStatement = DirectCast(getAndSetMethods.GetMethodDeclaration, MethodStatementSyntax)
            Dim setMethodStatement = TryCast(getAndSetMethods.SetMethodDeclaration, MethodStatementSyntax)

            Dim propertyNameToken = GetPropertyName(getMethodStatement.Identifier, propertyName, nameChanged)

            Dim newPropertyDeclaration As DeclarationStatementSyntax
            If getAndSetMethods.SetMethod Is Nothing Then
                Dim modifiers = getMethodStatement.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                Dim propertyStatement = SyntaxFactory.PropertyStatement(
                        getMethodStatement.AttributeLists, modifiers, propertyNameToken, Nothing,
                        getMethodStatement.AsClause, initializer:=Nothing, implementsClause:=getMethodStatement.ImplementsClause)

                If getAndSetMethods.GetMethodDeclaration.IsParentKind(SyntaxKind.FunctionBlock) Then
                    ' Get method has no body, and we have no setter.  Just make a readonly property block
                    Dim accessor = SyntaxFactory.GetAccessorBlock(SyntaxFactory.GetAccessorStatement(),
                        DirectCast(getAndSetMethods.GetMethodDeclaration.Parent, MethodBlockBaseSyntax).Statements)
                    Dim accessors = SyntaxFactory.SingletonList(accessor)
                    newPropertyDeclaration = SyntaxFactory.PropertyBlock(propertyStatement, accessors)
                Else
                    ' Get method has no body, and we have no setter.  Just make a readonly property statement
                    newPropertyDeclaration = propertyStatement
                End If
            Else
                Dim propertyStatement = SyntaxFactory.PropertyStatement(
                        getMethodStatement.AttributeLists, getMethodStatement.Modifiers, propertyNameToken, Nothing,
                        getMethodStatement.AsClause, initializer:=Nothing, implementsClause:=getMethodStatement.ImplementsClause)

                If getAndSetMethods.GetMethodDeclaration.IsParentKind(SyntaxKind.FunctionBlock) AndAlso
                    getAndSetMethods.SetMethodDeclaration.IsParentKind(SyntaxKind.SubBlock) Then

                    Dim getAccessor = SyntaxFactory.GetAccessorBlock(SyntaxFactory.GetAccessorStatement(),
                        DirectCast(getAndSetMethods.GetMethodDeclaration.Parent, MethodBlockBaseSyntax).Statements)

                    Dim setAccessorStatement = SyntaxFactory.SetAccessorStatement()
                    setAccessorStatement = setAccessorStatement.WithParameterList(setMethodStatement?.ParameterList)

                    If getAndSetMethods.GetMethod.DeclaredAccessibility <> getAndSetMethods.SetMethod.DeclaredAccessibility Then
                        setAccessorStatement = DirectCast(generator.WithAccessibility(setAccessorStatement, getAndSetMethods.SetMethod.DeclaredAccessibility), AccessorStatementSyntax)
                    End If

                    Dim setAccessor = SyntaxFactory.SetAccessorBlock(setAccessorStatement,
                        DirectCast(getAndSetMethods.SetMethodDeclaration.Parent, MethodBlockBaseSyntax).Statements)

                    Dim accessors = SyntaxFactory.List({getAccessor, setAccessor})
                    newPropertyDeclaration = SyntaxFactory.PropertyBlock(propertyStatement, accessors)
                Else
                    ' Methods don't have bodies.  Just make a property statement
                    newPropertyDeclaration = propertyStatement
                End If
            End If

            Dim trivia As IEnumerable(Of SyntaxTrivia) = getMethodStatement.GetLeadingTrivia()
            If setMethodStatement IsNot Nothing Then
                trivia = trivia.Concat(setMethodStatement.GetLeadingTrivia())
            End If

            newPropertyDeclaration = newPropertyDeclaration.WithLeadingTrivia(trivia)

            Return newPropertyDeclaration.WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Private Function GetPropertyName(identifier As SyntaxToken, propertyName As String, nameChanged As Boolean) As SyntaxToken
            Return If(nameChanged, SyntaxFactory.Identifier(propertyName), identifier)
        End Function

        Public Sub ReplaceGetReference(editor As SyntaxEditor, nameToken As SyntaxToken, propertyName As String, nameChanged As Boolean) Implements IReplaceMethodWithPropertyService.ReplaceGetReference
            If nameToken.Kind() <> SyntaxKind.IdentifierToken Then
                Return
            End If

            Dim nameNode = TryCast(nameToken.Parent, IdentifierNameSyntax)
            If nameNode Is Nothing Then
                Return
            End If

            Dim newName = If(nameChanged,
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName).WithTriviaFrom(nameToken)),
                nameNode)

            Dim parentExpression = If(nameNode.IsRightSideOfDot(), DirectCast(nameNode.Parent, ExpressionSyntax), nameNode)
            Dim root = If(parentExpression.IsParentKind(SyntaxKind.InvocationExpression), parentExpression.Parent, parentExpression)

            editor.ReplaceNode(root, parentExpression.ReplaceNode(nameNode, newName))
        End Sub

        Public Sub ReplaceSetReference(editor As SyntaxEditor, nameToken As SyntaxToken, propertyName As String, nameChanged As Boolean) Implements IReplaceMethodWithPropertyService.ReplaceSetReference
            If nameToken.Kind() <> SyntaxKind.IdentifierToken Then
                Return
            End If

            Dim nameNode = TryCast(nameToken.Parent, IdentifierNameSyntax)
            If nameNode Is Nothing Then
                Return
            End If

            Dim newName = If(nameChanged,
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName).WithTriviaFrom(nameToken)),
                nameNode)

            Dim parentExpression = If(nameNode.IsRightSideOfDot(), DirectCast(nameNode.Parent, ExpressionSyntax), nameNode)
            If Not parentExpression.IsParentKind(SyntaxKind.InvocationExpression) OrElse
               Not parentExpression.Parent.IsParentKind(SyntaxKind.ExpressionStatement) Then

                ' Wasn't invoked.  Change the name, but report a conflict.
                Dim annotation = ConflictAnnotation.Create(FeaturesResources.NonInvokedMethodCannotBeReplacedWithProperty)
                editor.ReplaceNode(nameNode, Function(n, g) newName.WithIdentifier(newName.Identifier.WithAdditionalAnnotations(annotation)))
                Return
            End If

            editor.ReplaceNode(
                parentExpression.Parent.Parent,
                Function(statement, generator)
                    Dim expressionStatement = DirectCast(statement, ExpressionStatementSyntax)
                    Dim invocationExpression = DirectCast(expressionStatement.Expression, InvocationExpressionSyntax)
                    Dim expression = invocationExpression.Expression
                    Dim name = If(expression.Kind() = SyntaxKind.SimpleMemberAccessExpression,
                        DirectCast(expression, MemberAccessExpressionSyntax).Name,
                        If(expression.Kind() = SyntaxKind.IdentifierName, DirectCast(expression, IdentifierNameSyntax), Nothing))

                    If name Is Nothing Then
                        Return statement
                    End If

                    If invocationExpression.ArgumentList?.Arguments.Count <> 1 Then
                        Return statement
                    End If

                    Dim result As SyntaxNode = SyntaxFactory.SimpleAssignmentStatement(
                        expression.ReplaceNode(name, newName),
                        invocationExpression.ArgumentList.Arguments(0).GetExpression())

                    Return result
                End Function)
        End Sub

        Private Shared Function IsInvocationName(nameNode As IdentifierNameSyntax, invocationExpression As ExpressionSyntax) As Boolean
            If invocationExpression Is nameNode Then
                Return True
            End If

            If nameNode.IsAnyMemberAccessExpressionName() AndAlso nameNode.Parent Is invocationExpression Then
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace