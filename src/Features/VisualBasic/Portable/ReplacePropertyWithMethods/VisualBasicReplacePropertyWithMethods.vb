Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    <ExportLanguageService(GetType(IReplacePropertyWithMethodsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReplacePropertyWithMethods
        Inherits AbstractReplacePropertyWithMethodsService(Of IdentifierNameSyntax, ExpressionSyntax, StatementSyntax)

        Public Overrides Function GetPropertyDeclaration(token As SyntaxToken) As SyntaxNode
            Dim containingProperty = token.Parent.FirstAncestorOrSelf(Of PropertyStatementSyntax)
            If containingProperty Is Nothing Then
                Return Nothing
            End If

            ' a parameterized property can be trivially converted to a method.
            If containingProperty.ParameterList IsNot Nothing Then
                Return Nothing
            End If

            Dim start = If(containingProperty.AttributeLists.Count > 0,
                containingProperty.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart,
                 containingProperty.SpanStart)

            ' Offer this refactoring anywhere in the signature of the property.
            Dim position = token.SpanStart
            If position < start Then
                Return Nothing
            End If

            If containingProperty.HasReturnType() AndAlso
                position > containingProperty.GetReturnType().Span.End Then
                Return Nothing
            End If

            Return containingProperty
        End Function

        Public Overrides Function GetReplacementMembers(
                document As Document,
                [property] As IPropertySymbol,
                propertyDeclarationNode As SyntaxNode,
                propertyBackingField As IFieldSymbol,
                desiredGetMethodName As String,
                desiredSetMethodName As String,
                cancellationToken As CancellationToken) As IList(Of SyntaxNode)

            Dim propertyStatement = TryCast(propertyDeclarationNode, PropertyStatementSyntax)
            If propertyStatement Is Nothing Then
                Return SpecializedCollections.EmptyList(Of SyntaxNode)
            End If

            Return ConvertPropertyToMembers(
                SyntaxGenerator.GetGenerator(document), [property],
                propertyStatement, propertyBackingField,
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken)
        End Function

        Private Function ConvertPropertyToMembers(
                generator As SyntaxGenerator,
                [property] As IPropertySymbol,
                propertyStatement As PropertyStatementSyntax,
                propertyBackingField As IFieldSymbol,
                desiredGetMethodName As String,
                desiredSetMethodName As String,
                cancellationToken As CancellationToken) As IList(Of SyntaxNode)

            Dim result = New List(Of SyntaxNode)()

            If propertyBackingField IsNot Nothing Then
                Dim initializer = propertyStatement.Initializer?.Value
                result.Add(generator.FieldDeclaration(propertyBackingField, initializer))
            End If

            Dim getMethod = [property].GetMethod
            If getMethod IsNot Nothing Then
                result.Add(GetGetMethod(
                    generator, propertyStatement, propertyBackingField,
                    getMethod, desiredGetMethodName, cancellationToken))
            End If

            Dim setMethod = [property].SetMethod
            If setMethod IsNot Nothing Then
                result.Add(GetSetMethod(
                    generator, propertyStatement, propertyBackingField,
                    setMethod, desiredSetMethodName, cancellationToken))
            End If

            Return result
        End Function

        Private Function GetGetMethod(
                generator As SyntaxGenerator,
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
                statements.AddRange(block.Statements)
            ElseIf propertyBackingField IsNot Nothing Then
                Dim fieldReference = GetFieldReference(generator, propertyBackingField)
                statements.Add(generator.ReturnStatement(fieldReference))
            End If

            Return generator.MethodDeclaration(getMethod, desiredGetMethodName, statements)
        End Function

        Private Function GetSetMethod(
                generator As SyntaxGenerator,
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
                statements.AddRange(block.Statements)
            ElseIf propertyBackingField IsNot Nothing Then
                Dim fieldReference = GetFieldReference(generator, propertyBackingField)
                statements.Add(generator.AssignmentStatement(
                    fieldReference, generator.IdentifierName(setMethod.Parameters(0).Name)))
            End If

            Return generator.MethodDeclaration(setMethod, desiredSetMethodName, statements)
        End Function

        Public Overrides Function GetPropertyNodeToReplace(propertyDeclaration As SyntaxNode) As SyntaxNode
            ' In VB we'll have the property statement.  If that is parented by a 
            ' property block, we'll want to replace that instead.  Otherwise we
            ' just replace the property statement itself
            Return If(propertyDeclaration.IsParentKind(SyntaxKind.PropertyBlock),
                propertyDeclaration.Parent,
                propertyDeclaration)
        End Function

        Protected Overrides Function UnwrapCompoundAssignment(compoundAssignment As SyntaxNode, readExpression As ExpressionSyntax) As ExpressionSyntax
            Throw New InvalidOperationException("Compound assignments don't exist in VB")
        End Function
    End Class
End Namespace