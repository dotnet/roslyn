' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ReplaceMethodWithProperty
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    <ExportLanguageService(GetType(IReplaceMethodWithPropertyService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReplaceMethodWithPropertyService
        Inherits AbstractReplaceMethodWithPropertyService(Of MethodStatementSyntax)
        Implements IReplaceMethodWithPropertyService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Sub RemoveSetMethod(editor As SyntaxEditor, setMethodDeclaration As SyntaxNode) Implements IReplaceMethodWithPropertyService.RemoveSetMethod
            Dim setMethodStatement = TryCast(setMethodDeclaration, MethodStatementSyntax)
            If setMethodStatement Is Nothing Then
                Return
            End If

            Dim methodOrBlock = GetParentIfBlock(setMethodStatement)
            editor.RemoveNode(methodOrBlock)
        End Sub

        Public Sub ReplaceGetMethodWithProperty(
                options As CodeGenerationOptions,
                parseOptions As ParseOptions,
                editor As SyntaxEditor,
                semanticModel As SemanticModel,
                getAndSetMethods As GetAndSetMethods,
                propertyName As String, nameChanged As Boolean,
                cancellationToken As CancellationToken) Implements IReplaceMethodWithPropertyService.ReplaceGetMethodWithProperty

            Dim getMethodDeclaration = TryCast(getAndSetMethods.GetMethodDeclaration, MethodStatementSyntax)
            If getMethodDeclaration Is Nothing Then
                Return
            End If

            Dim methodBlockOrStatement = GetParentIfBlock(getMethodDeclaration)
            editor.ReplaceNode(methodBlockOrStatement,
                               ConvertMethodsToProperty(editor, getAndSetMethods, propertyName, nameChanged))
        End Sub

        Private Shared Function GetParentIfBlock(declaration As MethodStatementSyntax) As DeclarationStatementSyntax
            If declaration.IsParentKind(SyntaxKind.FunctionBlock) OrElse declaration.IsParentKind(SyntaxKind.SubBlock) Then
                Return DirectCast(declaration.Parent, DeclarationStatementSyntax)
            End If

            Return declaration
        End Function

        Private Shared Function ConvertMethodsToProperty(
            editor As SyntaxEditor,
            getAndSetMethods As GetAndSetMethods,
            propertyName As String, nameChanged As Boolean) As DeclarationStatementSyntax

            Dim generator = editor.Generator

            Dim getMethodStatement = DirectCast(getAndSetMethods.GetMethodDeclaration, MethodStatementSyntax)
            Dim setMethodStatement = TryCast(getAndSetMethods.SetMethodDeclaration, MethodStatementSyntax)

            Dim propertyNameToken = GetPropertyName(getMethodStatement.Identifier, propertyName, nameChanged)
            Dim warning = GetWarning(getAndSetMethods)
            If warning IsNot Nothing Then
                propertyNameToken = propertyNameToken.WithAdditionalAnnotations(WarningAnnotation.Create(warning))
            End If

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

            newPropertyDeclaration = SetLeadingTrivia(
                VisualBasicSyntaxFacts.Instance, getAndSetMethods, newPropertyDeclaration)

            Return newPropertyDeclaration.WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Private Shared Function GetPropertyName(identifier As SyntaxToken, propertyName As String, nameChanged As Boolean) As SyntaxToken
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

            editor.ReplaceNode(
                root,
                Function(c As SyntaxNode, g As SyntaxGenerator)
                    Dim currentRoot = DirectCast(c, ExpressionSyntax)
                    Dim expression = If(currentRoot.IsKind(SyntaxKind.InvocationExpression),
                                        DirectCast(currentRoot, InvocationExpressionSyntax).Expression,
                                        currentRoot)
                    Dim rightName = expression.GetRightmostName()
                    Return expression.ReplaceNode(rightName, newName.WithTrailingTrivia(currentRoot.GetTrailingTrivia()))
                End Function)
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
                Dim annotation = ConflictAnnotation.Create(FeaturesResources.Non_invoked_method_cannot_be_replaced_with_property)
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

        Private Function IReplaceMethodWithPropertyService_GetMethodDeclarationAsync(context As CodeRefactoringContext) As Task(Of SyntaxNode) Implements IReplaceMethodWithPropertyService.GetMethodDeclarationAsync
            Return GetMethodDeclarationAsync(context)
        End Function
    End Class
End Namespace
