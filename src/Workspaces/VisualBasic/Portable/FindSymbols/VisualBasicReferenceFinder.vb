' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Public Async Function DetermineCascadedSymbolsAsync([property] As IPropertySymbol, project As Project, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol)) Implements ILanguageServiceReferenceFinder.DetermineCascadedSymbolsAsync
            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim relatedSymbol = [property].FindRelatedExplicitlyDeclaredSymbol(compilation)

            Return If([property].Equals(relatedSymbol),
                SpecializedCollections.EmptyEnumerable(Of ISymbol),
                SpecializedCollections.SingletonEnumerable(relatedSymbol))
        End Function

        Public Async Function DetermineCascadedSymbolsAsync(namedType As INamedTypeSymbol, project As Project, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of ISymbol)) Implements ILanguageServiceReferenceFinder.DetermineCascadedSymbolsAsync
            Debug.Assert(project.Language = LanguageNames.VisualBasic)
            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)

            ' If this is a WinForms project, then the VB 'my' feature may have synthesized 
            ' a property that would return an instance of the main Form type for the project.
            ' Search for such properties and cascade to them as well.
            Dim myPropertySymbols = GetMatchingMyPropertySymbols(namedType, DirectCast(compilation, VisualBasicCompilation), cancellationToken).Distinct().ToList()
            If myPropertySymbols.Count > 0 Then
                Return myPropertySymbols
            End If

            Return Nothing
        End Function

        Private Function GetMatchingMyPropertySymbols(namedType As INamedTypeSymbol, compilation As VisualBasicCompilation, cancellationToken As CancellationToken) As IEnumerable(Of IPropertySymbol)
            Return From childNamespace In compilation.RootNamespace.GetNamespaceMembers()
                   Where childNamespace.IsMyNamespace(compilation)
                   From type In childNamespace.GetAllTypes(cancellationToken)
                   Where type.Name = "MyForms"
                   From childProperty In type.GetMembers().OfType(Of IPropertySymbol)
                   Where childProperty.IsImplicitlyDeclared AndAlso childProperty.Type.Equals(namedType)
                   Select childProperty
        End Function
    End Class
End Namespace