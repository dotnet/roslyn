Imports System.Collections.Generic
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Collections
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class VbCoreSymbolManager

        Friend NotInheritable Class VbCoreNamedTypeSymbol
            Inherits SourceNamedTypeSymbol

            Private ReadOnly _kind As EmbeddedSymbolKind

            Public Sub New(decl As MergedTypeDeclaration, containingSymbol As NamespaceOrTypeSymbol, containingModule As SourceModuleSymbol, kind As EmbeddedSymbolKind)
                MyBase.New(decl, containingSymbol, containingModule)

#If DEBUG Then
                Dim references As IEnumerable(Of SyntaxReference) = decl.SyntaxReferences
                Debug.Assert(references.Count = 1)
                Debug.Assert(references.First.SyntaxTree.IsVbCoreSyntaxTree())
#End If

                Debug.Assert(kind <> VisualBasic.EmbeddedSymbolKind.None)
                _kind = kind
            End Sub

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
                Get
                    Return _kind
                End Get
            End Property

            Protected Overrides Function GetMembersForCci() As ReadOnlyArray(Of Symbol)
                Dim builder = ArrayBuilder(Of Symbol).GetInstance()
                Dim manager As VbCoreSymbolManager = DirectCast(Me.ContainingAssembly, SourceAssemblySymbol).Compilation.VbCoreSymbolManager
                For Each member In Me.GetMembers
                    If manager.IsSymbolReferenced(member) Then
                        builder.Add(member)
                    End If
                Next
                Return builder.ToReadOnlyAndFree()
            End Function

        End Class
    End Class
End Namespace