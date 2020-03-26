' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.FindSymbols.Finders
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.FindSymbols
    <ExportLanguageService(GetType(ILanguageServiceReferenceFinder), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReferenceFinder
        Implements ILanguageServiceReferenceFinder

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function DetermineCascadedSymbolsAsync(symbolAndProjectId As SymbolAndProjectId,
                                                      project As Project,
                                                      cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndProjectId)) Implements ILanguageServiceReferenceFinder.DetermineCascadedSymbolsAsync
            Dim symbol = symbolAndProjectId.Symbol
            If symbol.Kind = SymbolKind.Property Then
                Return DetermineCascadedSymbolsAsync(
                    symbolAndProjectId.WithSymbol(DirectCast(symbol, IPropertySymbol)),
                    project, cancellationToken)
            ElseIf symbol.Kind = SymbolKind.NamedType Then
                Return DetermineCascadedSymbolsAsync(
                    symbolAndProjectId.WithSymbol(DirectCast(symbol, INamedTypeSymbol)),
                    project, cancellationToken)
            Else
                Return Task.FromResult(ImmutableArray(Of SymbolAndProjectId).Empty)
            End If
        End Function

        Private Async Function DetermineCascadedSymbolsAsync(
                [propertyAndProjectId] As SymbolAndProjectId(Of IPropertySymbol),
                project As Project,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndProjectId))

            Dim [property] = propertyAndProjectId.Symbol
            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim relatedSymbol = [property].FindRelatedExplicitlyDeclaredSymbol(compilation)

            Return If([property].Equals(relatedSymbol),
                ImmutableArray(Of SymbolAndProjectId).Empty,
                ImmutableArray.Create(
                    SymbolAndProjectId.Create(relatedSymbol, project.Id)))
        End Function

        Private Async Function DetermineCascadedSymbolsAsync(
                namedType As SymbolAndProjectId(Of INamedTypeSymbol),
                project As Project,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndProjectId))

            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)

            ' If this is a WinForms project, then the VB 'my' feature may have synthesized 
            ' a property that would return an instance of the main Form type for the project.
            ' Search for such properties and cascade to them as well.
            Return GetMatchingMyPropertySymbols(namedType, project.Id, compilation, cancellationToken).
                Distinct().ToImmutableArray()
        End Function

        Private Function GetMatchingMyPropertySymbols(
                namedType As SymbolAndProjectId(Of INamedTypeSymbol),
                projectId As ProjectId,
                compilation As Compilation,
                cancellationToken As CancellationToken) As IEnumerable(Of SymbolAndProjectId)
            Return From childNamespace In compilation.RootNamespace.GetNamespaceMembers()
                   Where childNamespace.IsMyNamespace(compilation)
                   From type In childNamespace.GetAllTypes(cancellationToken)
                   Where type.Name = "MyForms"
                   From childProperty In type.GetMembers().OfType(Of IPropertySymbol)
                   Where childProperty.IsImplicitlyDeclared AndAlso childProperty.Type.Equals(namedType.Symbol)
                   Select SymbolAndProjectId.Create(DirectCast(childProperty, ISymbol), projectId)
        End Function
    End Class
End Namespace
