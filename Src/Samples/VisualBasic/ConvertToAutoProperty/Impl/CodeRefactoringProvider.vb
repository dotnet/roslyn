' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System.ComponentModel.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeRefactoringProvider("ConvertToAutoPropertyVB", LanguageNames.VisualBasic)>
Class ConvertToAutoPropertyCodeRefactoringProvider
    Inherits CodeRefactoringProvider

    Public NotOverridable Overrides Async Function GetRefactoringsAsync(document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CodeAction))
        Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim token = root.FindToken(textSpan.Start)
        If token.Parent Is Nothing Then
            Return Nothing
        End If

        Dim propertyBlock = token.Parent.FirstAncestorOrSelf(Of PropertyBlockSyntax)()
        If propertyBlock Is Nothing OrElse Not HasBothAccessors(propertyBlock) OrElse Not propertyBlock.Span.IntersectsWith(textSpan.Start) Then
            Return Nothing
        End If

        ' TODO: Check that the property can be converted to an auto-property.
        ' It should be a simple property with a getter and setter that simply retrieves
        ' and assigns a backing field. In addition, the backing field should be private.

        Return {New ConvertToAutopropertyCodeAction("Convert to auto property", Function(c) ConvertToAutoAsync(document, propertyBlock, c))}
    End Function

    ''' <summary> 
    ''' Returns true if both get and set accessors exist with a single statement on the given property; otherwise false. 
    ''' </summary>  
    Private Shared Function HasBothAccessors(propertyBlock As PropertyBlockSyntax) As Boolean
        Dim accessors = propertyBlock.Accessors
        Dim getter = accessors.FirstOrDefault(Function(node) node.VisualBasicKind() = SyntaxKind.PropertyGetBlock)
        Dim setter = accessors.FirstOrDefault(Function(node) node.VisualBasicKind() = SyntaxKind.PropertySetBlock)

        Return getter IsNot Nothing AndAlso setter IsNot Nothing
    End Function

    Private Async Function ConvertToAutoAsync(document As Document, propertyBlock As PropertyBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
        ' First, annotate the property block so that we can get back to it later.
        Dim propertyAnnotation = New SyntaxAnnotation()
        Dim oldRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim newRoot = oldRoot.ReplaceNode(propertyBlock, propertyBlock.WithAdditionalAnnotations(propertyAnnotation))

        document = document.WithSyntaxRoot(newRoot)

        ' Find the backing field of the property
        Dim backingField = Await GetBackingFieldAsync(document, propertyAnnotation, cancellationToken).ConfigureAwait(False)

        ' Retrieve the initializer of the backing field
        Dim modifiedIdentifier = CType(backingField.DeclaringSyntaxReferences.Single().GetSyntax(), ModifiedIdentifierSyntax)
        Dim variableDeclarator = CType(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
        Dim initializer = variableDeclarator.Initializer

        ' Update all references to the backing field to point to the property name
        document = Await UpdateBackingFieldReferencesAsync(document, backingField, propertyAnnotation, cancellationToken).ConfigureAwait(False)

        ' Remove the backing field declaration
        document = Await RemoveBackingFieldDeclarationAsync(document, backingField, cancellationToken).ConfigureAwait(False)

        ' Finally, replace the property with an auto property
        document = Await ReplacePropertyWithAutoPropertyAsync(document, initializer, propertyAnnotation, cancellationToken).ConfigureAwait(False)

        Return document
    End Function

    Private Shared Async Function GetAnnotatedPropertyBlockAsync(document As Document, propertyAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of PropertyBlockSyntax)
        Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim annotatedNode = root.GetAnnotatedNodesAndTokens(propertyAnnotation).Single().AsNode()
        Return CType(annotatedNode, PropertyBlockSyntax)
    End Function

    Private Async Function GetBackingFieldAsync(document As Document, propertyAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of IFieldSymbol)
        Dim propertyBlock = Await GetAnnotatedPropertyBlockAsync(document, propertyAnnotation, cancellationToken).ConfigureAwait(False)
        Dim propertyGetter = propertyBlock.Accessors.FirstOrDefault(Function(node) node.VisualBasicKind() = SyntaxKind.PropertyGetBlock)

        Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
        Dim containingType = semanticModel.GetDeclaredSymbol(propertyBlock).ContainingType

        Dim statements = propertyGetter.Statements
        If statements.Count = 1 Then
            Dim returnStatement = TryCast(statements.FirstOrDefault(), ReturnStatementSyntax)

            If returnStatement IsNot Nothing AndAlso returnStatement.Expression IsNot Nothing Then
                Dim symbol = semanticModel.GetSymbolInfo(returnStatement.Expression).Symbol
                Dim fieldSymbol = TryCast(symbol, IFieldSymbol)

                If fieldSymbol IsNot Nothing AndAlso fieldSymbol.ContainingType.Equals(containingType) Then
                    Return fieldSymbol
                End If
            End If
        End If

        Return Nothing
    End Function

    Private Async Function UpdateBackingFieldReferencesAsync(document As Document, backingField As IFieldSymbol, propertyAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of Document)
        Dim propertyBlock = Await GetAnnotatedPropertyBlockAsync(document, propertyAnnotation, cancellationToken).ConfigureAwait(False)
        Dim propertyName = propertyBlock.PropertyStatement.Identifier.ValueText

        Dim oldRoot = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), SyntaxNode)
        Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

        Dim referenceRewriter = New ReferenceRewriter(propertyName, backingField, semanticModel)
        Dim newRoot = referenceRewriter.Visit(oldRoot)

        Return document.WithSyntaxRoot(newRoot)
    End Function

    Private Async Function RemoveBackingFieldDeclarationAsync(document As Document, backingField As IFieldSymbol, cancellationToken As CancellationToken) As Task(Of Document)
        Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
        backingField = SymbolFinder.FindSimilarSymbols(backingField, compilation, cancellationToken).FirstOrDefault()
        If backingField Is Nothing Then
            Return document
        End If

        Dim modifiedIdentifier = CType(backingField.DeclaringSyntaxReferences.Single().GetSyntax(), ModifiedIdentifierSyntax)
        Dim variableDeclarator = CType(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
        Dim fieldDeclaration = CType(variableDeclarator.Parent, FieldDeclarationSyntax)

        Dim oldRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

        Dim newRoot As SyntaxNode = Nothing
        If variableDeclarator.Names.Count > 1 Then
            Dim newVariableDeclarator = variableDeclarator.RemoveNode(modifiedIdentifier, SyntaxRemoveOptions.KeepExteriorTrivia)
            newVariableDeclarator = newVariableDeclarator.WithAdditionalAnnotations(Formatter.Annotation)
            newRoot = oldRoot.ReplaceNode(variableDeclarator, newVariableDeclarator)
        ElseIf fieldDeclaration.Declarators.Count > 1 Then
            Dim newFieldDeclaration = fieldDeclaration.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepExteriorTrivia)
            newFieldDeclaration = newFieldDeclaration.WithAdditionalAnnotations(Formatter.Annotation)
            newRoot = oldRoot.ReplaceNode(fieldDeclaration, newFieldDeclaration)
        Else
            newRoot = oldRoot.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepExteriorTrivia)
        End If

        Return document.WithSyntaxRoot(newRoot)
    End Function

    Private Shared Async Function ReplacePropertyWithAutoPropertyAsync(document As Document, initializer As EqualsValueSyntax, propertyAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of Document)
        Dim propertyBlock = Await GetAnnotatedPropertyBlockAsync(document, propertyAnnotation, cancellationToken).ConfigureAwait(False)

        Dim autoProperty = propertyBlock.PropertyStatement _
            .WithInitializer(initializer) _
            .WithAdditionalAnnotations(Formatter.Annotation)

        Dim oldRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
        Dim newRoot = oldRoot.ReplaceNode(Of SyntaxNode)(propertyBlock, autoProperty)

        Return document.WithSyntaxRoot(newRoot)
    End Function

    Private Class ConvertToAutopropertyCodeAction
        Inherits CodeAction

        Private generateDocument As Func(Of CancellationToken, Task(Of Document))
        Private _title As String

        Public Overrides ReadOnly Property Title As String
            Get
                Return _title
            End Get
        End Property

        Public Sub New(title As String, generateDocument As Func(Of CancellationToken, Task(Of Document)))
            Me._title = title
            Me.generateDocument = generateDocument
        End Sub

        Protected Overrides Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
            Return Me.generateDocument(cancellationToken)
        End Function
    End Class
End Class