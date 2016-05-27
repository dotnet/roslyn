Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ReplacePropertyWithMethods
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ReplaceMethodWithProperty
    <ExportLanguageService(GetType(IReplacePropertyWithMethodsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReplacePropertyWithMethods
        Implements IReplacePropertyWithMethodsService

        Public Function GetPropertyDeclaration(token As SyntaxToken) As SyntaxNode Implements IReplacePropertyWithMethodsService.GetPropertyDeclaration
            Dim containingProperty = token.Parent.FirstAncestorOrSelf(Of PropertyStatementSyntax)
            If containingProperty Is Nothing Then
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

            If position > containingProperty.ParameterList.Span.End Then
                Return Nothing
            End If

            Return containingProperty
        End Function

        Public Function GetReplacementMembers(
                document As Document,
                [property] As IPropertySymbol,
                propertyDeclarationNode As SyntaxNode,
                propertyBackingField As IFieldSymbol,
                desiredGetMethodName As String,
                desiredSetMethodName As String) As IList(Of SyntaxNode) Implements IReplacePropertyWithMethodsService.GetReplacementMembers

            Dim propertyStatement = TryCast(propertyDeclarationNode, PropertyStatementSyntax)
            If propertyStatement Is Nothing Then
                Return SpecializedCollections.EmptyList(Of SyntaxNode)
            End If


        End Function

        Public Sub ReplaceReference(editor As SyntaxEditor,
                                    nameToken As SyntaxToken,
                                    [property] As IPropertySymbol,
                                    propertyBackingField As IFieldSymbol,
                                    desiredGetMethodName As String,
                                    desiredSetMethodName As String) Implements IReplacePropertyWithMethodsService.ReplaceReference
            Throw New NotImplementedException()
        End Sub

        Public Function GetPropertyNodeToReplace(propertyDeclaration As SyntaxNode) As SyntaxNode Implements IReplacePropertyWithMethodsService.GetPropertyNodeToReplace
            ' In VB we'll have the property statement.  If that is parented by a 
            ' property block, we'll want to replace that instead.  Otherwise we
            ' just replace the property statement itself
            Return If(propertyDeclaration.IsParentKind(SyntaxKind.PropertyBlock),
                propertyDeclaration.Parent,
                propertyDeclaration)
        End Function
    End Class
End Namespace