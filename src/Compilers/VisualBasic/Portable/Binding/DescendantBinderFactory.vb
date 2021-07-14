' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Provides a way to obtain binders for descendant scopes in method or lambda body.
    ''' Factory for a method body does not create binders for scopes inside a lambda, 
    ''' contained by the method. A dedicated factory must be created for each lambda body.
    ''' </summary>
    Friend Class DescendantBinderFactory

        Private ReadOnly _rootBinder As ExecutableCodeBinder
        Private ReadOnly _root As SyntaxNode

        Public Sub New(binder As ExecutableCodeBinder, root As SyntaxNode)
            _rootBinder = binder
            _root = root
        End Sub

        Friend ReadOnly Property Root As SyntaxNode
            Get
                Return _root
            End Get
        End Property

        Friend ReadOnly Property RootBinder As ExecutableCodeBinder
            Get
                Return _rootBinder
            End Get
        End Property

        Friend Function GetBinder(node As SyntaxNode) As Binder
            Dim binder As BlockBaseBinder = Nothing
            If NodeToBinderMap.TryGetValue(node, binder) Then
                Return binder
            Else
                Return Nothing
            End If
        End Function

        Friend Function GetBinder(statementList As SyntaxList(Of StatementSyntax)) As Binder
            Dim binder As BlockBaseBinder = Nothing
            If StmtListToBinderMap.TryGetValue(statementList, binder) Then
                Return binder
            Else
                Return Nothing
            End If
        End Function

        Private _lazyNodeToBinderMap As ImmutableDictionary(Of SyntaxNode, BlockBaseBinder)

        ' Get the map that maps from syntax nodes to binders.
        Friend ReadOnly Property NodeToBinderMap As ImmutableDictionary(Of SyntaxNode, BlockBaseBinder)
            Get
                If _lazyNodeToBinderMap Is Nothing Then
                    BuildBinderMaps()
                End If
                Return _lazyNodeToBinderMap
            End Get
        End Property

        Private _lazyStmtListToBinderMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)

        ' Get the map that maps from statement lists to binders.
        Friend ReadOnly Property StmtListToBinderMap As ImmutableDictionary(Of SyntaxList(Of StatementSyntax), BlockBaseBinder)
            Get
                If _lazyStmtListToBinderMap Is Nothing Then
                    BuildBinderMaps()
                End If
                Return _lazyStmtListToBinderMap
            End Get
        End Property

        ' Build the two maps that map to nodes and statement lists to binders.
        Private Sub BuildBinderMaps()
            Dim builder As New LocalBinderBuilder(DirectCast(_rootBinder.ContainingMember, MethodSymbol))
            builder.MakeBinder(Root, RootBinder)
            Interlocked.CompareExchange(_lazyNodeToBinderMap, builder.NodeToBinderMap, Nothing)
            Interlocked.CompareExchange(_lazyStmtListToBinderMap, builder.StmtListToBinderMap, Nothing)
        End Sub

    End Class

End Namespace
