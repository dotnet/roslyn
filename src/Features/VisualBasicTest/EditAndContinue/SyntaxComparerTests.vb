' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <[UseExportProvider]>
    Public Class SyntaxComparerTests
        Inherits BasicTestBase

        Private Shared Function MakeLiteral(n As Integer) As SyntaxNode
            Return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(n))
        End Function

        <Fact>
        Public Sub GetSequenceEdits1()
            Dim edits = SyntaxComparer.GetSequenceEdits(
                {MakeLiteral(0), MakeLiteral(1), MakeLiteral(2)},
                {MakeLiteral(1), MakeLiteral(3)})

            AssertEx.Equal(
            {
                New SequenceEdit(2, -1),
                New SequenceEdit(-1, 1),
                New SequenceEdit(1, 0),
                New SequenceEdit(0, -1)
            }, edits, itemInspector:=Function(e) e.GetTestAccessor().GetDebuggerDisplay())
        End Sub

        <Fact>
        Public Sub GetSequenceEdits2()
            Dim edits = SyntaxComparer.GetSequenceEdits(
                ImmutableArray.Create(MakeLiteral(0), MakeLiteral(1), MakeLiteral(2)),
                ImmutableArray.Create(MakeLiteral(1), MakeLiteral(3)))

            AssertEx.Equal(
            {
                New SequenceEdit(2, -1),
                New SequenceEdit(-1, 1),
                New SequenceEdit(1, 0),
                New SequenceEdit(0, -1)
            }, edits, itemInspector:=Function(e) e.GetTestAccessor().GetDebuggerDisplay())
        End Sub

        <Fact>
        Public Sub GetSequenceEdits3()
            Dim edits = SyntaxComparer.GetSequenceEdits(
                 {SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)},
                 {SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)})

            AssertEx.Equal(
            {
                New SequenceEdit(2, 2),
                New SequenceEdit(1, -1),
                New SequenceEdit(0, 1),
                New SequenceEdit(-1, 0)
            }, edits, itemInspector:=Function(e) e.GetTestAccessor().GetDebuggerDisplay())
        End Sub

        <Fact>
        Public Sub GetSequenceEdits4()
            Dim edits = SyntaxComparer.GetSequenceEdits(
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)),
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)))

            AssertEx.Equal(
            {
                New SequenceEdit(2, 2),
                New SequenceEdit(1, -1),
                New SequenceEdit(0, 1),
                New SequenceEdit(-1, 0)
            }, edits, itemInspector:=Function(e) e.GetTestAccessor().GetDebuggerDisplay())
        End Sub

        <Fact>
        Public Sub ComputeDistance_Nodes()
            Dim distance = SyntaxComparer.ComputeDistance(
                 {MakeLiteral(0), MakeLiteral(1), MakeLiteral(2)},
                 {MakeLiteral(1), MakeLiteral(3)})

            Assert.Equal(0.67, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance_Tokens()
            Dim distance = SyntaxComparer.ComputeDistance(
                 {SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)},
                 {SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)})

            Assert.Equal(0.33, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance4()
            Dim distance = SyntaxComparer.ComputeDistance(
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)),
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)))

            Assert.Equal(0.33, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance_Token()
            Dim distance = SyntaxComparer.ComputeDistance(SyntaxFactory.Literal("abc", "abc"), SyntaxFactory.Literal("acb", "acb"))
            Assert.Equal(0.33, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance_Node()
            Dim distance = SyntaxComparer.ComputeDistance(MakeLiteral(101), MakeLiteral(150))
            Assert.Equal(1.0, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance_Null()
            Dim distance = SyntaxComparer.ComputeDistance(
                Nothing,
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))

            Assert.Equal(1.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                Nothing,
                ImmutableArray.Create(MakeLiteral(0)))

            Assert.Equal(1.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                Nothing,
                Array.Empty(Of SyntaxNode))

            Assert.Equal(0.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                Array.Empty(Of SyntaxNode),
                Nothing)

            Assert.Equal(0.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                Nothing,
                Array.Empty(Of SyntaxToken))

            Assert.Equal(0.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                Array.Empty(Of SyntaxToken),
                Nothing)

            Assert.Equal(0.0, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance_LongSequences()
            Dim t1 = SyntaxFactory.Token(SyntaxKind.PublicKeyword)
            Dim t2 = SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
            Dim t3 = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)

            Dim distance = SyntaxComparer.ComputeDistance(
                Enumerable.Range(0, 10000).Select(Function(i) If(i < 2000, t1, t2)),
                Enumerable.Range(0, 10000).Select(Function(i) If(i < 2000, t1, t3)))

            ' long sequences are indistinguishable if they have common prefix shorter then threshold
            Assert.Equal(0.0, distance)
        End Sub
    End Class
End Namespace
