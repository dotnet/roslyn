' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    <ExportLanguageService(GetType(IReplacePropertyWithMethodsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicReplacePropertyWithMethods
        Inherits AbstractReplacePropertyWithMethodsService(Of IdentifierNameSyntax, ExpressionSyntax, CrefReferenceSyntax, StatementSyntax, PropertyStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Function GetReplacementMembersAsync(
                document As Document,
                [property] As IPropertySymbol,
                propertyDeclarationNode As SyntaxNode,
                propertyBackingField As IFieldSymbol,
                desiredGetMethodName As String,
                desiredSetMethodName As String,
                fallbackOptions As CodeGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SyntaxNode))

            Dim propertyStatement = TryCast(propertyDeclarationNode, PropertyStatementSyntax)
            If propertyStatement Is Nothing Then
                Return SpecializedTasks.EmptyImmutableArray(Of SyntaxNode)
            End If

            Return Task.FromResult(ConvertPropertyToMembers(
                SyntaxGenerator.GetGenerator(document), [property],
                propertyStatement, propertyBackingField,
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken))
        End Function

        Private Shared Function ConvertPropertyToMembers(
                generator As SyntaxGenerator,
                [property] As IPropertySymbol,
                propertyStatement As PropertyStatementSyntax,
                propertyBackingField As IFieldSymbol,
                desiredGetMethodName As String,
                desiredSetMethodName As String,
                cancellationToken As CancellationToken) As ImmutableArray(Of SyntaxNode)

            Dim result = ArrayBuilder(Of SyntaxNode).GetInstance()
            If propertyBackingField IsNot Nothing Then
                Dim initializer = propertyStatement.Initializer?.Value
                result.Add(generator.FieldDeclaration(propertyBackingField, initializer))
            End If

            Dim getMethod = [property].GetMethod
            If getMethod IsNot Nothing Then
                result.Add(GetGetMethod(
                    generator, [property], propertyStatement, propertyBackingField,
                    getMethod, desiredGetMethodName, cancellationToken:=cancellationToken))
            End If

            Dim setMethod = [property].SetMethod
            If setMethod IsNot Nothing Then
                result.Add(GetSetMethod(
                    generator, [property], propertyStatement, propertyBackingField,
                    setMethod, desiredSetMethodName, cancellationToken:=cancellationToken))
            End If

            Return result.ToImmutableAndFree()
        End Function

        Private Shared Function GetGetMethod(
                generator As SyntaxGenerator,
                [property] As IPropertySymbol,
                propertyStatement As PropertyStatementSyntax,
                propertyBackingField As IFieldSymbol,
                getMethod As IMethodSymbol,
                desiredGetMethodName As String,
                cancellationToken As CancellationToken) As SyntaxNode
            Dim statements = New List(Of SyntaxNode)()

            Dim getAccessorDeclaration = If(getMethod.DeclaringSyntaxReferences.Length = 0,
                Nothing,
                TryCast(getMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax))

            If TypeOf getAccessorDeclaration?.Parent Is AccessorBlockSyntax Then
                Dim block = DirectCast(getAccessorDeclaration.Parent, AccessorBlockSyntax)
                statements.AddRange(block.Statements.Select(AddressOf WithFormattingAnnotation))
            ElseIf propertyBackingField IsNot Nothing Then
                Dim fieldReference = GetFieldReference(generator, propertyBackingField)
                statements.Add(generator.ReturnStatement(fieldReference))
            End If

            getMethod = UpdateExplicitInterfaceImplementations([property], getMethod, desiredGetMethodName)
            Dim methodDeclaration = generator.MethodDeclaration(getMethod, desiredGetMethodName, statements)

            methodDeclaration = CopyLeadingTriviaOver(propertyStatement, methodDeclaration, ConvertValueToReturnsRewriter.instance)
            Return methodDeclaration
        End Function

        Private Shared Function WithFormattingAnnotation(statement As StatementSyntax) As StatementSyntax
            Return statement.WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Private Shared Function GetSetMethod(
                generator As SyntaxGenerator,
                [property] As IPropertySymbol,
                propertyStatement As PropertyStatementSyntax,
                propertyBackingField As IFieldSymbol,
                setMethod As IMethodSymbol,
                desiredSetMethodName As String,
                cancellationToken As CancellationToken) As SyntaxNode
            Dim statements = New List(Of SyntaxNode)()

            Dim setAccessorDeclaration = If(setMethod.DeclaringSyntaxReferences.Length = 0,
                Nothing,
                TryCast(setMethod.DeclaringSyntaxReferences(0).GetSyntax(cancellationToken), AccessorStatementSyntax))

            If TypeOf setAccessorDeclaration?.Parent Is AccessorBlockSyntax Then
                Dim block = DirectCast(setAccessorDeclaration.Parent, AccessorBlockSyntax)
                statements.AddRange(block.Statements.Select(AddressOf WithFormattingAnnotation))
            ElseIf propertyBackingField IsNot Nothing Then
                Dim fieldReference = GetFieldReference(generator, propertyBackingField)
                statements.Add(generator.AssignmentStatement(
                    fieldReference, generator.IdentifierName(setMethod.Parameters(0).Name)))
            End If

            setMethod = UpdateExplicitInterfaceImplementations([property], setMethod, desiredSetMethodName)
            Dim methodDeclaration = generator.MethodDeclaration(setMethod, desiredSetMethodName, statements)

            methodDeclaration = CopyLeadingTriviaOver(propertyStatement, methodDeclaration, ConvertValueToParamRewriter.instance)
            Return methodDeclaration
        End Function

        Private Shared Function UpdateExplicitInterfaceImplementations(
                [property] As IPropertySymbol,
                method As IMethodSymbol,
                desiredName As String) As IMethodSymbol

            ' We have a property like:
            '       Public ReadOnly Goo As Integer Implements I.Goo'
            '
            ' That property has an implicit getter:
            '       Public Function get_Goo As Integer Implements I.get_Goo'
            '
            ' We want to generate the new explicit function:
            '       Public Function GetGoo() As Integer Implements I.GetGoo
            ' 
            ' To do this we make the new method using the information from the implicit getter 
            ' Function.  However, we need to update the 'explicit interface implementations' 
            ' of the implicit getter function so that they point to the updated interface method
            ' and not the old implicit interface method.
            Dim updatedImplementations = method.ExplicitInterfaceImplementations.SelectAsArray(
                Function(i) UpdateExplicitInterfaceImplementation([property], i, desiredName))

            Return If(updatedImplementations.SequenceEqual(method.ExplicitInterfaceImplementations),
                      method,
                      CodeGenerationSymbolFactory.CreateMethodSymbol(
                        method, explicitInterfaceImplementations:=updatedImplementations))
        End Function

        Private Shared Function UpdateExplicitInterfaceImplementation(
                [property] As IPropertySymbol,
                explicitInterfaceImplMethod As IMethodSymbol,
                desiredName As String) As IMethodSymbol

            If explicitInterfaceImplMethod IsNot Nothing Then
                If explicitInterfaceImplMethod.Name = "get_" + [property].Name OrElse
                   explicitInterfaceImplMethod.Name = "set_" + [property].Name Then
                    Return CodeGenerationSymbolFactory.CreateMethodSymbol(
                        explicitInterfaceImplMethod, name:=desiredName, containingType:=explicitInterfaceImplMethod.ContainingType)
                End If
            End If

            Return explicitInterfaceImplMethod
        End Function

        Private Shared Function CopyLeadingTriviaOver(propertyStatement As PropertyStatementSyntax,
                                               methodDeclaration As SyntaxNode,
                                               documentationCommentRewriter As VisualBasicSyntaxRewriter) As SyntaxNode
            Return methodDeclaration.WithLeadingTrivia(
                propertyStatement.GetLeadingTrivia().Select(Function(trivia) ConvertTrivia(trivia, documentationCommentRewriter)))
        End Function

        Private Shared Function ConvertTrivia(trivia As SyntaxTrivia, documentationCommentRewriter As VisualBasicSyntaxRewriter) As SyntaxTrivia
            If trivia.Kind() = SyntaxKind.DocumentationCommentTrivia Then
                Dim converted = documentationCommentRewriter.Visit(trivia.GetStructure())
                Return SyntaxFactory.Trivia(DirectCast(converted, StructuredTriviaSyntax))
            End If

            Return trivia
        End Function

        ''' <summary>
        ''' Used by the documentation comment rewriters to identify top-level <c>&lt;value&gt;</c> nodes.
        ''' </summary>
        Private Shared Function IsValueName(node As XmlNodeSyntax) As Boolean
            Dim name = TryCast(node, XmlNameSyntax)
            Return name?.Prefix Is Nothing AndAlso name?.LocalName.ValueText = "value"
        End Function

        Public Overrides Function GetPropertyNodeToReplace(propertyDeclaration As SyntaxNode) As SyntaxNode
            ' In VB we'll have the property statement.  If that is parented by a 
            ' property block, we'll want to replace that instead.  Otherwise we
            ' just replace the property statement itself
            Return If(propertyDeclaration.IsParentKind(SyntaxKind.PropertyBlock),
                propertyDeclaration.Parent,
                propertyDeclaration)
        End Function

        Protected Overrides Function TryGetCrefSyntax(identifierName As IdentifierNameSyntax) As CrefReferenceSyntax
            Dim simpleNameCref = TryCast(identifierName.Parent, CrefReferenceSyntax)
            If simpleNameCref IsNot Nothing Then
                Return simpleNameCref
            End If

            Dim qualifiedName = TryCast(identifierName.Parent, QualifiedNameSyntax)
            If qualifiedName Is Nothing Then
                Return Nothing
            End If

            Return TryCast(qualifiedName.Parent, CrefReferenceSyntax)
        End Function

        Protected Overrides Function CreateCrefSyntax(originalCref As CrefReferenceSyntax, identifierToken As SyntaxToken, parameterType As SyntaxNode) As CrefReferenceSyntax
            Dim signature As CrefSignatureSyntax
            Dim parameterSyntax = TryCast(parameterType, TypeSyntax)
            If parameterSyntax IsNot Nothing Then
                signature = SyntaxFactory.CrefSignature(SyntaxFactory.CrefSignaturePart(modifier:=Nothing, type:=parameterSyntax))
            Else
                signature = SyntaxFactory.CrefSignature()
            End If

            Dim typeReference As TypeSyntax = SyntaxFactory.IdentifierName(identifierToken)
            Dim qualifiedType = TryCast(originalCref.Name, QualifiedNameSyntax)
            If qualifiedType IsNot Nothing Then
                typeReference = qualifiedType.ReplaceNode(qualifiedType.GetLastDottedName(), typeReference)
            End If

            Return SyntaxFactory.CrefReference(typeReference, signature, asClause:=Nothing)
        End Function

        Protected Overrides Function UnwrapCompoundAssignment(compoundAssignment As SyntaxNode, readExpression As ExpressionSyntax) As ExpressionSyntax
            Throw New InvalidOperationException("Compound assignments don't exist in VB")
        End Function
    End Class
End Namespace
