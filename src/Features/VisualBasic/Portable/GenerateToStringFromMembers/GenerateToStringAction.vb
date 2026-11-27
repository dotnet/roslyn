' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Shared.Extensions

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateToStringFromMembers
    Friend NotInheritable Class GenerateToStringAction
        Inherits CodeAction

        Private ReadOnly _document As Document
        Private ReadOnly _typeDeclaration As SyntaxNode
        Private ReadOnly _containingType As INamedTypeSymbol
        Private ReadOnly _selectedMembers As ImmutableArray(Of ISymbol)

        Public Sub New(
            document As Document,
            typeDeclaration As SyntaxNode,
            containingType As INamedTypeSymbol,
            selectedMembers As ImmutableArray(Of ISymbol))

            _document = document
            _typeDeclaration = typeDeclaration
            _containingType = containingType
            _selectedMembers = selectedMembers
        End Sub

        Public Overrides ReadOnly Property Title As String
            Get
                Return FeaturesResources.Generate_ToString
            End Get
        End Property

        Public Overrides ReadOnly Property EquivalenceKey As String
            Get
                Return Title
            End Get
        End Property

        Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await _document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim generator = _document.GetRequiredLanguageService(Of SyntaxGenerator)()
            Dim generatorInternal = _document.GetRequiredLanguageService(Of SyntaxGeneratorInternal)()

            Dim method = ToStringGenerator.CreateStandaloneToStringMethod(
                generator, generatorInternal, compilation, _containingType, _selectedMembers)

            Dim info = Await _document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(False)
            Dim formattingOptions = Await _document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(False)

            Dim newTypeDeclaration = info.Service.AddMembers(_typeDeclaration, New IMethodSymbol() {method}, info, cancellationToken)

            Dim newDocument = Await UpdateDocumentAndAddImportsAsync(_typeDeclaration, newTypeDeclaration, cancellationToken).ConfigureAwait(False)

            Return Await Formatter.FormatAsync(newDocument, formattingOptions, cancellationToken).ConfigureAwait(False)
        End Function

        Private Async Function UpdateDocumentAndAddImportsAsync(oldType As SyntaxNode, newType As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            Dim oldRoot = Await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newDocument = _document.WithSyntaxRoot(oldRoot.ReplaceNode(oldType, newType))
            Dim addImportOptions = Await _document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(False)

            newDocument = Await ImportAdder.AddImportsFromSymbolAnnotationAsync(newDocument, addImportOptions, cancellationToken).ConfigureAwait(False)
            Return newDocument
        End Function
    End Class
End Namespace
