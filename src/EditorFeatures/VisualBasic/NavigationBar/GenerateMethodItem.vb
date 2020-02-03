' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Friend Class GenerateMethodItem
        Inherits AbstractGenerateCodeItem

        Private ReadOnly _destinationTypeSymbolKey As SymbolKey
        Private ReadOnly _methodToReplicateSymbolKey As SymbolKey

        Public Sub New(text As String, glyph As Glyph, destinationTypeSymbolId As SymbolKey, methodToReplicateSymbolId As SymbolKey)
            MyBase.New(text, glyph)

            _destinationTypeSymbolKey = destinationTypeSymbolId
            _methodToReplicateSymbolKey = methodToReplicateSymbolId
        End Sub

        Protected Overrides Async Function GetGeneratedDocumentCoreAsync(document As Document, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(_destinationTypeSymbolKey.Resolve(compilation).Symbol, INamedTypeSymbol)
            Dim methodToReplicate = TryCast(_methodToReplicateSymbolKey.Resolve(compilation).Symbol, IMethodSymbol)

            If destinationType Is Nothing OrElse methodToReplicate Is Nothing Then
                Return Nothing
            End If

            Dim codeGenerationSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(
                CodeGenerationSymbolFactory.CreateMethodSymbol(
                    methodToReplicate.RemoveInaccessibleAttributesAndAttributesOfTypes(destinationType)))

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 codeGenerationSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
