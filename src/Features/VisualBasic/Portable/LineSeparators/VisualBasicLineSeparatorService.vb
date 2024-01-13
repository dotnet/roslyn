' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LineSeparators
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineSeparators
    <ExportLanguageService(GetType(ILineSeparatorService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicLineSeparatorService
        Implements ILineSeparatorService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        ''' <summary>Node types that are interesting for line separation.</summary>
        Private Shared Function IsSeparableBlock(nodeOrToken As SyntaxNodeOrToken) As Boolean
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
        Public Async Function GetLineSeparatorsAsync(
                document As Document,
                textSpan As TextSpan,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TextSpan)) Implements ILineSeparatorService.GetLineSeparatorsAsync
            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim root = Await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            Dim spans = ArrayBuilder(Of TextSpan).GetInstance()

            Dim blocks = root.Traverse(Of SyntaxNode)(textSpan, AddressOf IsSeparableContainer)

            For Each block In blocks
                If cancellationToken.IsCancellationRequested Then
                    Return ImmutableArray(Of TextSpan).Empty
                End If

                Dim typeBlock = TryCast(block, TypeBlockSyntax)
                If typeBlock IsNot Nothing Then
                    ProcessNodeList(typeBlock.Members, spans, cancellationToken)
                    Continue For
                End If

                Dim enumBlock = TryCast(block, EnumBlockSyntax)
                If enumBlock IsNot Nothing Then
                    ProcessNodeList(enumBlock.Members, spans, cancellationToken)
                    Continue For
                End If

                Dim nsBlock = TryCast(block, NamespaceBlockSyntax)
                If nsBlock IsNot Nothing Then
                    ProcessNodeList(nsBlock.Members, spans, cancellationToken)
                    Continue For
                End If

                Dim progBlock = TryCast(block, CompilationUnitSyntax)
                If progBlock IsNot Nothing Then
                    ProcessImports(progBlock.Imports, spans)
                    ProcessNodeList(progBlock.Members, spans, cancellationToken)
                End If
            Next

            Return spans.ToImmutable()
        End Function

        ''' <summary>
        ''' If node is separable and not the last in its container => add line separator after the node
        ''' If node is separable and not the first in its container => ensure separator before the node
        ''' last separable node in Program needs separator after it.
        ''' </summary>
        Private Shared Sub ProcessNodeList(Of T As SyntaxNode)(children As SyntaxList(Of T), spans As ArrayBuilder(Of TextSpan), token As CancellationToken)
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
                        spans.Add(GetLineSeparatorSpanForNode(prev))
                    End If

                    spans.Add(GetLineSeparatorSpanForNode(cur))

                    seenSeparator = True
                End If
            Next

            ' last child may need separator only before it
            Dim lastChild = children.Last()

            If IsSeparableBlock(lastChild) Then
                If Not seenSeparator Then
                    Dim nextToLast = children(children.Count - 2)
                    spans.Add(GetLineSeparatorSpanForNode(nextToLast))
                End If

                If lastChild.Parent.Kind() = SyntaxKind.CompilationUnit Then
                    spans.Add(GetLineSeparatorSpanForNode(lastChild))
                End If
            End If

        End Sub

        Private Shared Sub ProcessImports(importsList As SyntaxList(Of ImportsStatementSyntax), spans As ArrayBuilder(Of TextSpan))
            If importsList.Any() Then
                spans.Add(GetLineSeparatorSpanForNode(importsList.Last()))
            End If
        End Sub

        Private Shared Function GetLineSeparatorSpanForNode(node As SyntaxNode) As TextSpan
            Contract.ThrowIfNull(node)

            ' PERF: Reverse the list to only realize the last child
            Dim lastToken = node.ChildNodesAndTokens().Reverse().First()
            Return lastToken.Span
        End Function
    End Class
End Namespace
