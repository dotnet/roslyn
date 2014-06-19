Imports System.Collections.Generic
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' A structure used to lexically order symbols. For performance, it's important that this be 
    ''' a STRUCTURE, and be able to be returned from a symbol without doing any additional allocations (even
    ''' if nothing is cached yet.)
    ''' </summary>
    Friend Structure LexicalSortKey
        Public ReadOnly EmbeddedKind As EmbeddedSymbolKind

        ''' <summary>
        ''' If Nothing, symbol is in metadata or embedded or otherwise not it source.
        ''' </summary>
        Public ReadOnly Tree As SyntaxTree

        ''' <summary>
        ''' Position within the tree. Doesn't need to exactly match the span returned by Locations, just
        ''' be good enough to sort. In other words, we don't need to go to extra work to return the location of the identifier,
        ''' just some syntax location is fine.
        ''' </summary>
        Public ReadOnly Position As Integer

        Public Sub New(embeddedKind As EmbeddedSymbolKind, tree As SyntaxTree, location As Integer)
            Me.EmbeddedKind = embeddedKind
            Me.Tree = tree
            Me.Position = location
        End Sub

        Public Sub New(tree As SyntaxTree, location As Integer)
            Me.EmbeddedKind = If(tree Is Nothing, EmbeddedSymbolKind.None, EmbeddedSymbolManager.GetEmbeddedKind(tree))
            Me.Tree = tree
            Me.Position = location
        End Sub

        Public Sub New(syntaxRef As SyntaxReference)
            Me.New(syntaxRef.SyntaxTree, syntaxRef.Span.Start)
        End Sub

        ' WARNING: Only use this if the location is obtainable without allocating it (even if cached later). E.g., only
        ' if the location object is stored in the constructor of the symbol.
        Public Sub New(location As Location)
            Me.New(location.EmbeddedKind, location.SourceTree, location.PossiblyEmbeddedSourceSpan.Start)
        End Sub

        ' WARNING: Only use this if the node is obtainable without allocating it (even if cached later). E.g., only
        ' if the node is stored in the constructor of the symbol. In particular, do not call this on the result of a GetSyntax()
        ' call on a SyntacReference.
        Public Sub New(node As SyntaxNode)
            Me.New(node.SyntaxTree, node.Span.Start)
        End Sub

        ' WARNING: Only use this if the token is obtainable without allocating it (even if cached later). E.g., only
        ' if the node is stored in the constructor of the symbol. In particular, do not call this on the result of a GetSyntax()
        ' call on a SyntaxReference.
        Public Sub New(token As SyntaxToken)
            Me.New(token.SyntaxTree, token.Span.Start)
        End Sub

        ' A location not in source.
        Public Shared ReadOnly NotInSource As LexicalSortKey = New LexicalSortKey(Nothing, 0)
    End Structure
End Namespace
