Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Editor.VisualBasic.Utilities

Namespace Roslyn.Services.Editor.VisualBasic.Extensions
    Friend Module CompilationExtensions
        <Extension()>
        Public Function GetNamespaces(compilation As Compilation, namespaceName As NameSyntax) As IEnumerable(Of NamespaceSymbol)
            Contract.ThrowIfNull(compilation)

            Dim rootNamespaceCollection = SpecializedCollections.SingletonEnumerable(Of NamespaceSymbol)(compilation.GlobalNamespace)
            If namespaceName Is Nothing Then
                ' root namespace
                Return rootNamespaceCollection
            End If

            Dim currentNamespaceSymbols = rootNamespaceCollection
            For Each nameNode In New NameSyntaxIterator(namespaceName)
                Dim childNamespaceName = nameNode.GetText()
                If String.IsNullOrEmpty(childNamespaceName) Then
                    ' invalid input, return empty colleciton
                    Return SpecializedCollections.EmptyEnumerable(Of NamespaceSymbol)()
                End If

                Dim childNamespaceSymbols = From currentSymbol In currentNamespaceSymbols
                                            From childNamespaceSymbol In compilation.GetNamespaces(currentSymbol, childNamespaceName)
                                            Select childNamespaceSymbol

                currentNamespaceSymbols = childNamespaceSymbols
            Next

            Return currentNamespaceSymbols
        End Function

        <Extension()>
        Public Function GetNamespaces(compilation As Compilation, containingNamespaceSymbol As NamespaceSymbol, childNamespaceName As String) As IEnumerable(Of NamespaceSymbol)
            Contract.ThrowIfNull(compilation)
            Contract.ThrowIfNull(containingNamespaceSymbol)
            Contract.ThrowIfNull(childNamespaceName)

            Return containingNamespaceSymbol.GetMembers(childNamespaceName).OfType(Of NamespaceSymbol)()
        End Function
    End Module
End Namespace