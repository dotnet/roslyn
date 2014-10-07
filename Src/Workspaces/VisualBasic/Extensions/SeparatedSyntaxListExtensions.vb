Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SeparatedSyntaxListExtensions
        <Extension()>
        Public Function ToSeparatedSyntaxList(Of T As SyntaxNode)(sequence As IEnumerable(Of T), token As SyntaxToken, Optional includeTrailingSeparator As Boolean = False) As SeparatedSyntaxList(Of T)

            Dim list = sequence.ToList()
            Dim count = If(includeTrailingSeparator, list.Count, Math.Max(0, list.Count - 1))
            Return SyntaxFactory.SeparatedList(Of T)(list, Enumerable.Repeat(token, count))
        End Function

        <Extension()>
        Public Function ToSeparatedSyntaxList(Of T As SyntaxNode)(sequence As IEnumerable(Of T), syntaxKind As SyntaxKind,
            Optional includeTrailingSeparator As Boolean = False) As SeparatedSyntaxList(Of T)
            Return ToSeparatedSyntaxList(sequence, SyntaxFactory.Token(syntaxKind), includeTrailingSeparator)
        End Function

        <Extension()>
        Public Function Concat(Of T As SyntaxNode)(sequence1 As SeparatedSyntaxList(Of T), sequence2 As SeparatedSyntaxList(Of T), separatorToken As SyntaxToken) As SeparatedSyntaxList(Of T)
            Dim values = New List(Of T)
            Dim separators = New List(Of SyntaxToken)

            values.AddRange(sequence1)
            values.AddRange(sequence2)

            sequence1.AppendSeparators(separators)

            ' if a separator is missing between two separated list
            If sequence1.Count > 0 AndAlso
               sequence1.SeparatorCount < sequence1.Count AndAlso
               sequence2.Count > 0 Then
                separators.Add(separatorToken)
            End If

            sequence2.AppendSeparators(separators)

            Return SyntaxFactory.SeparatedList(values, separators)
        End Function

        <Extension()>
        Public Sub AppendSeparators(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), separators As List(Of SyntaxToken))
            For i = 0 To separatedList.SeparatorCount - 1
                separators.Add(separatedList.GetSeparator(i))
            Next
        End Sub

        <Extension()>
        Public Function GetSeparators(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T)) As IEnumerable(Of SyntaxToken)
            Dim list = New List(Of SyntaxToken)
            separatedList.AppendSeparators(list)

            Return list
        End Function

        <Extension()>
        Public Function ToNodeSeparatorPair(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), Optional separatorToken As SyntaxToken = Nothing) As IEnumerable(Of Tuple(Of T, SyntaxToken))
            Contract.ThrowIfFalse(separatedList.Count = separatedList.SeparatorCount OrElse separatedList.Count = separatedList.SeparatorCount + 1)
            Dim nodeAndSeparatorPairs = New List(Of Tuple(Of T, SyntaxToken))()

            For i = 0 To separatedList.Count - 1
                Dim token = If(i < separatedList.SeparatorCount, separatedList.GetSeparator(i), separatorToken)
                nodeAndSeparatorPairs.Add(New Tuple(Of T, SyntaxToken)(separatedList(i), token))
            Next

            Return nodeAndSeparatorPairs
        End Function

        <Extension()>
        Private Function GetNodesAndSeparators(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T)) As Tuple(Of List(Of T), List(Of SyntaxToken))
            Contract.Requires(separatedList.Count = separatedList.SeparatorCount OrElse
                              separatedList.Count = separatedList.SeparatorCount + 1)

            Dim nodes As New List(Of T)(separatedList.Count)
            Dim separators As New List(Of SyntaxToken)(separatedList.SeparatorCount)

            For i = 0 To separatedList.Count - 1
                nodes.Add(separatedList(i))

                If i < separatedList.SeparatorCount Then
                    separators.Add(separatedList.GetSeparator(i))
                End If
            Next

            Return Tuple.Create(nodes, separators)
        End Function

        <Extension()>
        Public Function Insert(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), index As Integer, node As T, separator As SyntaxKind) As SeparatedSyntaxList(Of T)
            Return Insert(separatedList, index, node, SyntaxFactory.Token(separator))
        End Function

        <Extension()>
        Public Function Insert(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), index As Integer, node As T, separator As SyntaxToken) As SeparatedSyntaxList(Of T)

            Contract.Requires(index >= 0 AndAlso index <= separatedList.Count)

            Dim nodesAndSeparators = separatedList.GetNodesAndSeparators()

            Dim nodes = nodesAndSeparators.Item1
            Dim separators = nodesAndSeparators.Item2

            nodes.Insert(index, node)

            Dim separatorIndex = If(index <= separatedList.SeparatorCount, index, separatedList.SeparatorCount)
            separators.Insert(separatorIndex, separator)

            Return SyntaxFactory.SeparatedList(nodes, separators)
        End Function

        <Extension()>
        Public Function Remove(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), node As T) As SeparatedSyntaxList(Of T)
            Dim nodesAndSeparators = separatedList.GetNodesAndSeparators()

            Dim nodes = nodesAndSeparators.Item1
            Dim separators = nodesAndSeparators.Item2

            For i = 0 To separatedList.Count - 1
                If separatedList(i) Is node Then

                    Dim trailing = nodes(i).GetTrailingTrivia()
                    nodes.RemoveAt(i)

                    If separatedList.SeparatorCount > 0 Then
                        Dim separatorIndex = If(i < separatedList.SeparatorCount, i, separatedList.SeparatorCount - 1)
                        separators.RemoveAt(separatorIndex)
                    End If

                    If (i > 0) Then
                        nodes(i - 1) = nodes(i - 1).WithAppendedTrailingTrivia(trailing)
                    End If
                    Exit For
                End If
            Next

            Return SyntaxFactory.SeparatedList(nodes, separators)
        End Function

        <Extension()>
        Public Function Replace(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), oldNode As T, newNode As T) As SeparatedSyntaxList(Of T)
            Dim nodesAndSeparators = separatedList.GetNodesAndSeparators()

            Dim nodes = nodesAndSeparators.Item1
            Dim separators = nodesAndSeparators.Item2

            For i = 0 To separatedList.Count - 1
                If separatedList(i) Is oldNode Then
                    nodes(i) = newNode
                    Exit For
                End If
            Next

            Return SyntaxFactory.SeparatedList(nodes, separators)
        End Function

        <Extension()>
        Public Function Remove(Of T As SyntaxNode)(separatedList As SeparatedSyntaxList(Of T), index As Integer, length As Integer) As SeparatedSyntaxList(Of T)
            Contract.ThrowIfFalse(separatedList.Count = separatedList.SeparatorCount OrElse separatedList.Count = separatedList.SeparatorCount + 1)

            Dim nodesAndSeparators = separatedList.ToNodeSeparatorPair().Where(
                Function(p, i)
                    If i < index Then
                        Return True
                    End If
                    If i < index + length Then
                        Return False
                    End If
                    Return True
                End Function)

            Return SyntaxFactory.SeparatedList(nodesAndSeparators.Select(Function(p) p.Item1), nodesAndSeparators.Where(Function(p) p.Item2.VisualBasicKind <> SyntaxKind.None).Select(Function(p) p.Item2))
        End Function
    End Module
End Namespace