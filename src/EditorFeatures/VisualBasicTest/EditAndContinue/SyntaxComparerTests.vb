' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
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
            }, edits, itemInspector:=Function(e) e.GetDebuggerDisplay())
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
            }, edits, itemInspector:=Function(e) e.GetDebuggerDisplay())
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
            }, edits, itemInspector:=Function(e) e.GetDebuggerDisplay())
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
            }, edits, itemInspector:=Function(e) e.GetDebuggerDisplay())
        End Sub

        <Fact>
        Public Sub ComputeDistance1()
            Dim distance = SyntaxComparer.ComputeDistance(
                 {MakeLiteral(0), MakeLiteral(1), MakeLiteral(2)},
                 {MakeLiteral(1), MakeLiteral(3)})

            Assert.Equal(0.67, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance2()
            Dim distance = SyntaxComparer.ComputeDistance(
                ImmutableArray.Create(MakeLiteral(0), MakeLiteral(1), MakeLiteral(2)),
                ImmutableArray.Create(MakeLiteral(1), MakeLiteral(3)))

            Assert.Equal(0.67, Math.Round(distance, 2))
        End Sub

        <Fact>
        Public Sub ComputeDistance3()
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
                SpecializedCollections.EmptyArray(Of SyntaxNode))

            Assert.Equal(0.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                SpecializedCollections.EmptyArray(Of SyntaxNode),
                Nothing)

            Assert.Equal(0.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                Nothing,
                SpecializedCollections.EmptyArray(Of SyntaxToken))

            Assert.Equal(0.0, Math.Round(distance, 2))

            distance = SyntaxComparer.ComputeDistance(
                SpecializedCollections.EmptyArray(Of SyntaxToken),
                Nothing)

            Assert.Equal(0.0, Math.Round(distance, 2))
        End Sub
    End Class
End Namespace
