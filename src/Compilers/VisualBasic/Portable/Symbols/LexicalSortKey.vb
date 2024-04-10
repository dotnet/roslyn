' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A structure used to lexically order symbols. For performance, it's important that this be 
    ''' a STRUCTURE, and be able to be returned from a symbol without doing any additional allocations (even
    ''' if nothing is cached yet.)
    ''' </summary>
    Friend Structure LexicalSortKey

        <Flags()>
        Private Enum SyntaxTreeKind As Byte
            None = EmbeddedSymbolKind.None
            Unset = EmbeddedSymbolKind.Unset
            EmbeddedAttribute = EmbeddedSymbolKind.EmbeddedAttribute
            VbCore = EmbeddedSymbolKind.VbCore
            XmlHelper = EmbeddedSymbolKind.XmlHelper

            MyTemplate = EmbeddedSymbolKind.LastValue << 1
        End Enum

        Private _embeddedKind As SyntaxTreeKind
        Private _treeOrdinal As Integer
        Private _position As Integer

        ''' <summary>
        ''' Embedded kind of the tree. 
        ''' </summary>
        Private ReadOnly Property EmbeddedKind As SyntaxTreeKind
            Get
                Return Me._embeddedKind
            End Get
        End Property

        ''' <summary>
        ''' If -1, symbol is in metadata or embedded or otherwise not it source.
        ''' Note that TreeOrdinal is only used for EmbeddedSymbolKind.None trees, thus
        ''' negative ordinals of embedded trees do not interfere
        ''' </summary>
        Public ReadOnly Property TreeOrdinal As Integer
            Get
                Return Me._treeOrdinal
            End Get
        End Property

        ''' <summary>
        ''' Position within the tree. Doesn't need to exactly match the span returned by Locations, just
        ''' be good enough to sort. In other words, we don't need to go to extra work to return the location of the identifier,
        ''' just some syntax location is fine.
        ''' 
        ''' Negative value indicates that the structure was not initialized yet, is used for lazy 
        ''' initializations only along with LexicalSortKey.NotInitialized
        ''' </summary>
        Public ReadOnly Property Position As Integer
            Get
                Return Me._position
            End Get
        End Property

        ' A location not in source.
        Public Shared ReadOnly NotInSource As LexicalSortKey = New LexicalSortKey(SyntaxTreeKind.None, -1, 0)

        ' A lexical sort key is not initialized yet
        Public Shared ReadOnly NotInitialized As LexicalSortKey = New LexicalSortKey() With {._embeddedKind = SyntaxTreeKind.None, ._treeOrdinal = -1, ._position = -1}

        Private Sub New(embeddedKind As SyntaxTreeKind, treeOrdinal As Integer, location As Integer)
            Debug.Assert(location >= 0)
            Debug.Assert(treeOrdinal >= -1)
            Debug.Assert(embeddedKind = EmbeddedSymbolKind.None OrElse treeOrdinal = -1)

            Me._embeddedKind = embeddedKind
            Me._treeOrdinal = treeOrdinal
            Me._position = location
        End Sub

        Private Sub New(embeddedKind As SyntaxTreeKind, tree As SyntaxTree, location As Integer, compilation As VisualBasicCompilation)
            Me.New(embeddedKind, If(tree Is Nothing OrElse embeddedKind <> SyntaxTreeKind.None, -1, compilation.GetSyntaxTreeOrdinal(tree)), location)
        End Sub

        Private Shared Function GetEmbeddedKind(tree As SyntaxTree) As SyntaxTreeKind
            Return If(tree Is Nothing,
                      SyntaxTreeKind.None,
                      If(tree.IsMyTemplate,
                         SyntaxTreeKind.MyTemplate,
                         CType(tree.GetEmbeddedKind(), SyntaxTreeKind)))
        End Function

        Public Sub New(tree As SyntaxTree, position As Integer, compilation As VisualBasicCompilation)
            Me.New(GetEmbeddedKind(tree), tree, position, compilation)
        End Sub

        Public Sub New(syntaxRef As SyntaxReference, compilation As VisualBasicCompilation)
            Me.New(syntaxRef.SyntaxTree, syntaxRef.Span.Start, compilation)
        End Sub

        ''' <summary>
        ''' WARNING: Only use this if the location is obtainable without allocating it (even if cached later). E.g., only
        ''' if the location object is stored in the constructor of the symbol.
        ''' </summary>
        Public Sub New(location As Location, compilation As VisualBasicCompilation)
            If location Is Nothing Then
                Me._embeddedKind = SyntaxTreeKind.None
                Me._treeOrdinal = -1
                Me._position = 0
            Else
                Debug.Assert(location.PossiblyEmbeddedOrMySourceSpan.Start >= 0)

                Dim tree = DirectCast(location.PossiblyEmbeddedOrMySourceTree, VisualBasicSyntaxTree)
                Debug.Assert(tree Is Nothing OrElse tree.GetEmbeddedKind = location.EmbeddedKind)

                Dim treeKind As SyntaxTreeKind = GetEmbeddedKind(tree)

                If treeKind <> SyntaxTreeKind.None Then
                    Me._embeddedKind = treeKind
                    Me._treeOrdinal = -1
                Else
                    Me._embeddedKind = SyntaxTreeKind.None
                    Me._treeOrdinal = If(tree Is Nothing, -1, compilation.GetSyntaxTreeOrdinal(tree))
                End If

                Me._position = location.PossiblyEmbeddedOrMySourceSpan.Start
            End If
        End Sub

        ''' <summary>
        ''' WARNING: Only use this if the node is obtainable without allocating it (even if cached later). E.g., only
        ''' if the node is stored in the constructor of the symbol. In particular, do not call this on the result of a GetSyntax()
        ''' call on a SyntaxReference.
        ''' </summary>
        Public Sub New(node As VisualBasicSyntaxNode, compilation As VisualBasicCompilation)
            Me.New(node.SyntaxTree, node.SpanStart, compilation)
        End Sub

        ''' <summary>
        ''' WARNING: Only use this if the token is obtainable without allocating it (even if cached later). E.g., only
        ''' if the node is stored in the constructor of the symbol. In particular, do not call this on the result of a GetSyntax()
        ''' call on a SyntaxReference.
        ''' </summary>
        Public Sub New(token As SyntaxToken, compilation As VisualBasicCompilation)
            Me.New(DirectCast(token.SyntaxTree, VisualBasicSyntaxTree), token.SpanStart, compilation)
        End Sub

        ''' <summary>
        ''' Compare two lexical sort keys in a compilation.
        ''' </summary>
        Public Shared Function Compare(ByRef xSortKey As LexicalSortKey, ByRef ySortKey As LexicalSortKey) As Integer
            Debug.Assert(xSortKey.EmbeddedKind <> EmbeddedSymbolKind.Unset)
            Debug.Assert(ySortKey.EmbeddedKind <> EmbeddedSymbolKind.Unset)

            Dim comparison As Integer

            If xSortKey.EmbeddedKind <> ySortKey.EmbeddedKind Then
                ' Embedded sort before non-embedded.
                Return If(ySortKey.EmbeddedKind > xSortKey.EmbeddedKind, 1, -1)
            End If

            If xSortKey.EmbeddedKind = EmbeddedSymbolKind.None AndAlso xSortKey.TreeOrdinal <> ySortKey.TreeOrdinal Then
                If xSortKey.TreeOrdinal < 0 Then
                    Return 1
                ElseIf ySortKey.TreeOrdinal < 0 Then
                    Return -1
                End If

                comparison = xSortKey.TreeOrdinal - ySortKey.TreeOrdinal
                Debug.Assert(comparison <> 0)
                Return comparison
            End If

            Return xSortKey.Position - ySortKey.Position
        End Function

        Public Shared Function Compare(first As Location, second As Location, compilation As VisualBasicCompilation) As Integer
            Debug.Assert(first.IsInSource OrElse first.IsEmbeddedOrMyTemplateLocation())
            Debug.Assert(second.IsInSource OrElse second.IsEmbeddedOrMyTemplateLocation())

            ' This is a shortcut to avoid building complete keys for the case when both locations belong to the same tree.
            ' Also saves us in some speculative SemanticModel scenarios when the tree we are dealing with doesn't belong to
            ' the compilation and an attempt of building the LexicalSortKey will simply assert and crash.
            If first.SourceTree IsNot Nothing AndAlso first.SourceTree Is second.SourceTree Then
                Return first.PossiblyEmbeddedOrMySourceSpan.Start - second.PossiblyEmbeddedOrMySourceSpan.Start
            End If

            Dim firstKey = New LexicalSortKey(first, compilation)
            Dim secondKey = New LexicalSortKey(second, compilation)
            Return LexicalSortKey.Compare(firstKey, secondKey)
        End Function

        Public Shared Function Compare(first As SyntaxReference, second As SyntaxReference, compilation As VisualBasicCompilation) As Integer
            ' This is a shortcut to avoid building complete keys for the case when both locations belong to the same tree.
            ' Also saves us in some speculative SemanticModel scenarios when the tree we are dealing with doesn't belong to
            ' the compilation and an attempt of building the LexicalSortKey will simply assert and crash.
            If first.SyntaxTree IsNot Nothing AndAlso first.SyntaxTree Is second.SyntaxTree Then
                Return first.Span.Start - second.Span.Start
            End If

            Dim firstKey = New LexicalSortKey(first, compilation)
            Dim secondKey = New LexicalSortKey(second, compilation)
            Return LexicalSortKey.Compare(firstKey, secondKey)
        End Function

        Public Shared Function Compare(first As SyntaxNode, second As SyntaxNode, compilation As VisualBasicCompilation) As Integer
            ' This is a shortcut to avoid building complete keys for the case when both locations belong to the same tree.
            ' Also saves us in some speculative SemanticModel scenarios when the tree we are dealing with doesn't belong to
            ' the compilation and an attempt of building the LexicalSortKey will simply assert and crash.
            If first.SyntaxTree IsNot Nothing AndAlso first.SyntaxTree Is second.SyntaxTree Then
                Return first.Span.Start - second.Span.Start
            End If

            Dim firstKey = New LexicalSortKey(first.SyntaxTree, first.SpanStart, compilation)
            Dim secondKey = New LexicalSortKey(second.SyntaxTree, second.SpanStart, compilation)
            Return LexicalSortKey.Compare(firstKey, secondKey)
        End Function

        Public Shared Function First(xSortKey As LexicalSortKey, ySortKey As LexicalSortKey) As LexicalSortKey
            Dim comparison As Integer = Compare(xSortKey, ySortKey)
            Return If(comparison > 0, ySortKey, xSortKey)
        End Function

        Public ReadOnly Property IsInitialized As Boolean
            Get
                Return Volatile.Read(Me._position) >= 0
            End Get
        End Property

        Public Sub SetFrom(ByRef other As LexicalSortKey)
            Debug.Assert(other.IsInitialized)
            Me._embeddedKind = other._embeddedKind
            Me._treeOrdinal = other._treeOrdinal
            Volatile.Write(Me._position, other._position)
        End Sub

    End Structure
End Namespace
