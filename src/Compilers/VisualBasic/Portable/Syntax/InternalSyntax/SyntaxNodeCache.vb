' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' #Const STATS = True

Imports Roslyn.Utilities
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    ''' <summary>
    ''' Provides caching functionality for green nonterminals with up to 3 children.
    ''' Example:
    '''     When constructing a node with given kind, flags, child1 and child2, we can look up 
    '''     in the cache whether we already have a node that contains same kind, flags, 
    '''     child1 and child2 and use that.
    '''     
    '''     For the purpose of children comparison, reference equality is used as a much cheaper 
    '''     alternative to the structural/recursive equality. This implies that in order to de-duplicate
    '''     a node to a cache node, the children of two nodes must be already de-duplicated.     
    '''     When adding a node to the cache we verify that cache does contain node's children,
    '''     since otherwise there is no reason for the node to be used.
    '''     Tokens/nulls are for this purpose considered deduplicated. Indeed most of the tokens
    '''     are deduplicated via quick-scanner caching, so we just assume they all are.
    '''     
    '''     As a result of above, "fat" nodes with 4 or more children or their recursive parents
    '''     will never be in the cache. This naturally limits the typical single cache item to be 
    '''     a relatively simple expression. We do not want the cache to be completely unbounded 
    '''     on the item size. 
    '''     While it still may be possible to store a gigantic nested binary expression, 
    '''     it should be a rare occurrence.
    '''     
    '''     We only consider "normal" nodes to be cacheable. 
    '''     Nodes with diagnostics/annotations/directives/skipped, etc... have more complicated identity 
    '''     and are not likely to be repetitive.
    '''     
    ''' </summary>
    Friend Class GreenStats
        Private Shared s_stats As GreenStats = New GreenStats()

        ' TODO: remove when done tweaking this cache.
#If STATS Then
        Private greenNodes As Integer
        Private greenTokens As Integer
        Private nontermsAdded As Integer
        Private cacheableNodes As Integer
        Private cacheHits As Integer

        Friend Shared Sub NoteGreen(node As SyntaxNode)
            Interlocked.Increment(stats.greenNodes)
            If (node.IsToken) Then
                Interlocked.Increment(stats.greenTokens)
            End If
        End Sub

        Friend Shared Sub ItemAdded()
            Interlocked.Increment(stats.nontermsAdded)
        End Sub

        Friend Shared Sub ItemCacheable()
            Interlocked.Increment(stats.cacheableNodes)
        End Sub

        Friend Shared Sub CacheHit()
            Interlocked.Increment(stats.cacheHits)
        End Sub

        Protected Overrides Sub Finalize()
            Console.WriteLine("Green: " & greenNodes)
            Console.WriteLine("GreenTk: " & greenTokens)
            Console.WriteLine("Nonterminals added: " & nontermsAdded)
            Console.WriteLine("Nonterminals cacheable: " & cacheableNodes)
            Console.WriteLine("CacheHits: " & cacheHits)
            Console.WriteLine("RateOfAll: " & (cacheHits * 100 / (cacheHits + greenNodes - greenTokens)) & "%")
            Console.WriteLine("RateOfCacheable: " & (cacheHits * 100 / (cacheableNodes)) & "%")
        End Sub
#Else
        Friend Shared Sub NoteGreen(node As GreenNode)
        End Sub

        <Conditional("DEBUG")>
        Friend Shared Sub ItemAdded()
        End Sub

        <Conditional("DEBUG")>
        Friend Shared Sub ItemCacheable()
        End Sub

        <Conditional("DEBUG")>
        Friend Shared Sub CacheHit()
        End Sub
#End If
    End Class

    Friend Class SyntaxNodeCache
        Private Const s_cacheSizeBits As Integer = 16
        Private Const s_cacheSize As Integer = 1 << s_cacheSizeBits
        Private Const s_cacheMask As Integer = s_cacheSize - 1

        Private Structure Entry
            Public ReadOnly hash As Integer
            Public ReadOnly node As GreenNode

            Friend Sub New(hash As Integer, node As GreenNode)
                Me.hash = hash
                Me.node = node
            End Sub
        End Structure

        Private Shared ReadOnly s_cache As Entry() = New Entry(s_cacheSize - 1) {}

        Friend Shared Sub AddNode(node As GreenNode, hash As Integer)
            If (AllChildrenInCache(node) AndAlso Not node.IsMissing) Then
                GreenStats.ItemAdded()

                Debug.Assert(node.GetCacheHash() = hash)

                Dim idx As Integer = hash And s_cacheMask
                s_cache(idx) = New Entry(hash, node)
            End If
        End Sub

        Private Shared Function CanBeCached(child1 As GreenNode) As Boolean
            Return child1 Is Nothing OrElse child1.IsCacheable
        End Function

        Private Shared Function CanBeCached(child1 As GreenNode, child2 As GreenNode) As Boolean
            Return CanBeCached(child1) AndAlso CanBeCached(child2)
        End Function

        Private Shared Function CanBeCached(child1 As GreenNode, child2 As GreenNode, child3 As GreenNode) As Boolean
            Return CanBeCached(child1) AndAlso CanBeCached(child2) AndAlso CanBeCached(child3)
        End Function

        Private Shared Function ChildInCache(child As GreenNode) As Boolean
            ' for the purpose of this function consider that 
            ' null nodes, tokens and trivias are cached somewhere else.
            If (child Is Nothing OrElse child.SlotCount = 0) Then
                Return True
            End If

            Dim hash = child.GetCacheHash()
            Dim idx = hash And s_cacheMask
            Return s_cache(idx).node Is child
        End Function

        Private Shared Function AllChildrenInCache(node As GreenNode) As Boolean
            For i As Integer = 0 To node.SlotCount - 1
                If (Not ChildInCache(DirectCast(node.GetSlot(i), GreenNode))) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Friend Shared Function TryGetNode(kind As Integer, child1 As GreenNode, ByRef hash As Integer) As GreenNode
            Return TryGetNode(kind, child1, GetFlags(), hash)
        End Function

        Friend Shared Function TryGetNode(kind As Integer, context As ISyntaxFactoryContext, child1 As GreenNode, ByRef hash As Integer) As GreenNode
            Return TryGetNode(kind, child1, GetFlags(context), hash)
        End Function

        Private Shared Function TryGetNode(kind As Integer, child1 As GreenNode, flags As GreenNode.NodeFlags, ByRef hash As Integer) As GreenNode
            If (CanBeCached(child1)) Then
                GreenStats.ItemCacheable()

                Dim h As Integer = GetCacheHash(kind, flags, child1)
                hash = h
                Dim idx As Integer = h And s_cacheMask
                Dim e = s_cache(idx)
                If (e.hash = h AndAlso e.node IsNot Nothing AndAlso e.node.IsCacheEquivalent(kind, flags, child1)) Then
                    GreenStats.CacheHit()
                    Return e.node
                End If
            Else
                hash = -1
            End If

            Return Nothing
        End Function

        Friend Shared Function TryGetNode(kind As Integer, child1 As GreenNode, child2 As GreenNode, ByRef hash As Integer) As GreenNode
            Return TryGetNode(kind, child1, child2, GetFlags(), hash)
        End Function

        Friend Shared Function TryGetNode(kind As Integer, context As ISyntaxFactoryContext, child1 As GreenNode, child2 As GreenNode, ByRef hash As Integer) As GreenNode
            Return TryGetNode(kind, child1, child2, GetFlags(context), hash)
        End Function

        Private Shared Function TryGetNode(kind As Integer, child1 As GreenNode, child2 As GreenNode, flags As GreenNode.NodeFlags, ByRef hash As Integer) As GreenNode
            If (CanBeCached(child1, child2)) Then
                GreenStats.ItemCacheable()

                Dim h As Integer = GetCacheHash(kind, flags, child1, child2)
                hash = h
                Dim idx As Integer = h And s_cacheMask
                Dim e = s_cache(idx)
                If (e.hash = h AndAlso e.node IsNot Nothing AndAlso e.node.IsCacheEquivalent(kind, flags, child1, child2)) Then
                    GreenStats.CacheHit()
                    Return e.node
                End If
            Else
                hash = -1
            End If
            Return Nothing
        End Function

        Friend Shared Function TryGetNode(kind As Integer, child1 As GreenNode, child2 As GreenNode, child3 As GreenNode, ByRef hash As Integer) As GreenNode
            Return TryGetNode(kind, child1, child2, child3, GetFlags(), hash)
        End Function

        Friend Shared Function TryGetNode(kind As Integer, context As ISyntaxFactoryContext, child1 As GreenNode, child2 As GreenNode, child3 As GreenNode, ByRef hash As Integer) As GreenNode
            Return TryGetNode(kind, child1, child2, child3, GetFlags(context), hash)
        End Function

        Private Shared Function TryGetNode(kind As Integer, child1 As GreenNode, child2 As GreenNode, child3 As GreenNode, flags As GreenNode.NodeFlags, ByRef hash As Integer) As GreenNode
            If (CanBeCached(child1, child2, child3)) Then
                GreenStats.ItemCacheable()

                Dim h As Integer = GetCacheHash(kind, flags, child1, child2, child3)
                hash = h
                Dim idx As Integer = h And s_cacheMask
                Dim e = s_cache(idx)
                If (e.hash = h AndAlso e.node IsNot Nothing AndAlso e.node.IsCacheEquivalent(kind, flags, child1, child2, child3)) Then
                    GreenStats.CacheHit()
                    Return e.node
                End If
            Else
                hash = -1
            End If

            Return Nothing
        End Function

        Private Shared Function GetFlags() As GreenNode.NodeFlags
            Return GreenNode.NodeFlags.IsNotMissing
        End Function

        Private Shared Function GetFlags(context As ISyntaxFactoryContext) As GreenNode.NodeFlags
            Dim flags = GetFlags()
            flags = VisualBasicSyntaxNode.SetFactoryContext(flags, context)
            Return flags
        End Function

        Private Shared Function GetCacheHash(kind As Integer, flags As GreenNode.NodeFlags, child1 As GreenNode) As Integer
            Dim code As Integer = CInt(flags) Xor kind
            ' the only child is never null
            code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code)

            ' ensure nonnegative hash
            Return code And Int32.MaxValue
        End Function

        Private Shared Function GetCacheHash(kind As Integer, flags As GreenNode.NodeFlags, child1 As GreenNode, child2 As GreenNode) As Integer
            Dim code As Integer = CInt(flags) Xor kind

            If (child1 IsNot Nothing) Then
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code)
            End If
            If (child2 IsNot Nothing) Then
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child2), code)
            End If

            ' ensure nonnegative hash
            Return code And Int32.MaxValue
        End Function

        Private Shared Function GetCacheHash(kind As Integer, flags As GreenNode.NodeFlags, child1 As GreenNode, child2 As GreenNode, child3 As GreenNode) As Integer
            Dim code = CInt(flags) Xor CShort(kind)

            If (child1 IsNot Nothing) Then
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child1), code)
            End If
            If (child2 IsNot Nothing) Then
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child2), code)
            End If
            If (child3 IsNot Nothing) Then
                code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child3), code)
            End If

            ' ensure nonnegative hash
            Return code And Int32.MaxValue
        End Function
    End Class
End Namespace
