' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.FindSymbols.Finders
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.FindSymbols
    <ExportLanguageService(GetType(ILanguageServiceReferenceFinder), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicReferenceFinder
        Implements ILanguageServiceReferenceFinder

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function DetermineCascadedSymbolsAsync(
                symbol As ISymbol,
                project As Project,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol)) Implements ILanguageServiceReferenceFinder.DetermineCascadedSymbolsAsync
            If symbol.Kind = SymbolKind.Property Then
                Return DetermineCascadedSymbolsAsync(DirectCast(symbol, IPropertySymbol), project, cancellationToken)
            ElseIf symbol.Kind = SymbolKind.NamedType Then
                Return DetermineCascadedSymbolsAsync(DirectCast(symbol, INamedTypeSymbol), project, cancellationToken)
            Else
                Return SpecializedTasks.EmptyImmutableArray(Of ISymbol)
            End If
        End Function

        Private Shared Async Function DetermineCascadedSymbolsAsync(
                [property] As IPropertySymbol,
                project As Project,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))

            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim relatedSymbol = [property].FindRelatedExplicitlyDeclaredSymbol(compilation)

            Return If([property].Equals(relatedSymbol),
                ImmutableArray(Of ISymbol).Empty,
                ImmutableArray.Create(relatedSymbol))
        End Function

        Private Shared Async Function DetermineCascadedSymbolsAsync(
                namedType As INamedTypeSymbol,
                project As Project,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of ISymbol))

            Dim compilation = Await project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)

            ' If this is a WinForms project, then the VB 'my' feature may have synthesized 
            ' a property that would return an instance of the main Form type for the project.
            ' Search for such properties and cascade to them as well.

            Dim matchingMyPropertySymbols =
                From childNamespace In compilation.RootNamespace.GetNamespaceMembers()
                Where childNamespace.IsMyNamespace(compilation)
                From type In childNamespace.GetAllTypes(cancellationToken)
                Where type.Name = "MyForms"
                From childProperty In type.GetMembers().OfType(Of IPropertySymbol)
                Where childProperty.IsImplicitlyDeclared AndAlso childProperty.Type.Equals(namedType)
                Select DirectCast(childProperty, ISymbol)

            Return matchingMyPropertySymbols.Distinct().ToImmutableArray()
        End Function
    End Class
End Namespace
