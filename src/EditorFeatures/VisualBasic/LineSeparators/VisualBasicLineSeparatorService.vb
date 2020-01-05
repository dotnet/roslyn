' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineSeparators
    <ExportLanguageService(GetType(ILineSeparatorService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicLineSeparatorService
        Implements ILineSeparatorService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        ''' <summary>Node types that are interesting for line separation.</summary>
        Private Function IsSeparableBlock(nodeOrToken As SyntaxNodeOrToken) As Boolean
            If nodeOrToken.IsToken Then
                Return False
            End If

            Dim node = nodeOrToken.AsNode()
            Return _
                TypeOf (node) Is MethodBlockBaseSyntax OrElse
                TypeOf (node) Is PropertyBlockSyntax OrElse
                TypeOf (node) Is TypeBlockSyntax OrElse
                TypeOf (node) Is EnumBlockSyntax OrElse
                TypeOf (node) Is NamespaceBlockSyntax OrElse
                TypeOf (node) Is EventBlockSyntax
        End Function

        ''' <summary>Node types that may contain separable blocks.</summary>
        Private Function IsSeparableContainer(node As SyntaxNode) As Boolean
            Return _
                TypeOf node Is TypeBlockSyntax OrElse
                TypeOf node Is EnumBlockSyntax OrElse
                TypeOf node Is NamespaceBlockSyntax OrElse
                TypeOf node Is CompilationUnitSyntax
        End Function

        ''' <summary>
        ''' Given a syntaxTree returns line separator spans. The operation may take fairly long time
        ''' on a big syntaxTree so it is cancellable.
        ''' </summary>
        Public Async Function GetLineSeparatorsAsync(document As Document,
                                          textSpan As TextSpan,
                                          Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of TextSpan)) Implements ILineSeparatorService.GetLineSeparatorsAsync
            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim root = Await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            Dim spans As New List(Of TextSpan)

            Dim blocks = root.Traverse(Of SyntaxNode)(textSpan, AddressOf IsSeparableContainer)

            For Each block In blocks
                If cancellationToken.IsCancellationRequested Then
                    Return SpecializedCollections.EmptyList(Of TextSpan)()
                End If

                Dim typeBlock = TryCast(block, TypeBlockSyntax)
                If typeBlock IsNot Nothing Then
                    ProcessNodeList(syntaxTree, typeBlock.Members, spans, cancellationToken)
                    Continue For
                End If

                Dim enumBlock = TryCast(block, EnumBlockSyntax)
                If enumBlock IsNot Nothing Then
                    ProcessNodeList(syntaxTree, enumBlock.Members, spans, cancellationToken)
                    Continue For
                End If

                Dim nsBlock = TryCast(block, NamespaceBlockSyntax)
                If nsBlock IsNot Nothing Then
                    ProcessNodeList(syntaxTree, nsBlock.Members, spans, cancellationToken)
                    Continue For
                End If

                Dim progBlock = TryCast(block, CompilationUnitSyntax)
                If progBlock IsNot Nothing Then
                    ProcessImports(syntaxTree, progBlock.Imports, spans, cancellationToken)
                    ProcessNodeList(syntaxTree, progBlock.Members, spans, cancellationToken)
                End If
            Next

            Return spans
        End Function

        ''' <summary>
        ''' If node is separable and not the last in its container => add line separator after the node
        ''' If node is separable and not the first in its container => ensure separator before the node
        ''' last separable node in Program needs separator after it.
        ''' </summary>
        Private Sub ProcessNodeList(Of T As SyntaxNode)(syntaxTree As SyntaxTree, children As SyntaxList(Of T), spans As List(Of TextSpan), token As CancellationToken)
            Contract.ThrowIfNull(spans)

            If children.Count = 0 Then
                Return ' nothing to separate
            End If

            Dim seenSeparator As Boolean = True 'first child needs no separator
            For i As Integer = 0 To children.Count - 2
                token.ThrowIfCancellationRequested()

                Dim cur = children(i)
                If Not IsSeparableBlock(cur) Then
                    seenSeparator = False
                Else
                    If Not seenSeparator Then
                        Dim prev = children(i - 1)
                        spans.Add(GetLineSeparatorSpanForNode(syntaxTree, prev))
                    End If
                    spans.Add(GetLineSeparatorSpanForNode(syntaxTree, cur))

                    seenSeparator = True
                End If
            Next

            ' last child may need separator only before it
            Dim lastChild = children.Last()

            If IsSeparableBlock(lastChild) Then
                If Not seenSeparator Then
                    Dim nextToLast = children(children.Count - 2)
                    spans.Add(GetLineSeparatorSpanForNode(syntaxTree, nextToLast))
                End If
                If lastChild.Parent.Kind = SyntaxKind.CompilationUnit Then
                    spans.Add(GetLineSeparatorSpanForNode(syntaxTree, lastChild))
                End If
            End If

        End Sub

        Private Sub ProcessImports(syntaxTree As SyntaxTree, importsList As SyntaxList(Of ImportsStatementSyntax), spans As List(Of TextSpan), token As CancellationToken)
            If importsList.Any() Then
                spans.Add(GetLineSeparatorSpanForNode(syntaxTree, importsList.Last()))
            End If
        End Sub

        Private Function GetLineSeparatorSpanForNode(syntaxTree As SyntaxTree, node As SyntaxNode) As TextSpan
            Contract.ThrowIfNull(node)

            ' PERF: Reverse the list to only realize the last child
            Dim lastToken = node.ChildNodesAndTokens().Reverse().First()
            Return lastToken.Span
        End Function
    End Class
End Namespace
