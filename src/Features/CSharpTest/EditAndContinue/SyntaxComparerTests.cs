﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class SyntaxComparerTests
    {
        private static SyntaxNode MakeLiteral(int n)
            => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(n));

        [Fact]
        public void GetSequenceEdits1()
        {
            var edits = SyntaxComparer.GetSequenceEdits(
                new[] { MakeLiteral(0), MakeLiteral(1), MakeLiteral(2) },
                new[] { MakeLiteral(1), MakeLiteral(3) });

            AssertEx.Equal(new[]
            {
                new SequenceEdit(2, -1),
                new SequenceEdit(-1, 1),
                new SequenceEdit(1, 0),
                new SequenceEdit(0, -1),
            }, edits, itemInspector: e => e.GetTestAccessor().GetDebuggerDisplay());
        }

        [Fact]
        public void GetSequenceEdits2()
        {
            var edits = SyntaxComparer.GetSequenceEdits(
                ImmutableArray.Create(MakeLiteral(0), MakeLiteral(1), MakeLiteral(2)),
                ImmutableArray.Create(MakeLiteral(1), MakeLiteral(3)));

            AssertEx.Equal(new[]
            {
                new SequenceEdit(2, -1),
                new SequenceEdit(-1, 1),
                new SequenceEdit(1, 0),
                new SequenceEdit(0, -1),
            }, edits, itemInspector: e => e.GetTestAccessor().GetDebuggerDisplay());
        }

        [Fact]
        public void GetSequenceEdits3()
        {
            var edits = SyntaxComparer.GetSequenceEdits(
                new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword) },
                new[] { SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword) });

            AssertEx.Equal(new[]
            {
                new SequenceEdit(2, 2),
                new SequenceEdit(1, -1),
                new SequenceEdit(0, 1),
                new SequenceEdit(-1, 0),
            }, edits, itemInspector: e => e.GetTestAccessor().GetDebuggerDisplay());
        }

        [Fact]
        public void GetSequenceEdits4()
        {
            var edits = SyntaxComparer.GetSequenceEdits(
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)),
                ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)));

            AssertEx.Equal(new[]
            {
                new SequenceEdit(2, 2),
                new SequenceEdit(1, -1),
                new SequenceEdit(0, 1),
                new SequenceEdit(-1, 0),
            }, edits, itemInspector: e => e.GetTestAccessor().GetDebuggerDisplay());
        }

        [Fact]
        public void ComputeDistance_Nodes()
        {
            var distance = SyntaxComparer.ComputeDistance(
                new[] { MakeLiteral(0), MakeLiteral(1), MakeLiteral(2) },
                new[] { MakeLiteral(1), MakeLiteral(3) });

            Assert.Equal(0.67, Math.Round(distance, 2));
        }

        [Fact]
        public void ComputeDistance_Tokens()
        {
            var distance = SyntaxComparer.ComputeDistance(
                new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword) },
                new[] { SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword) });

            Assert.Equal(0.33, Math.Round(distance, 2));
        }

        [Fact]
        public void ComputeDistance_Token()
        {
            var distance = SyntaxComparer.ComputeDistance(SyntaxFactory.Literal("abc", "abc"), SyntaxFactory.Literal("acb", "acb"));
            Assert.Equal(0.33, Math.Round(distance, 2));
        }

        [Fact]
        public void ComputeDistance_Node()
        {
            var distance = SyntaxComparer.ComputeDistance(MakeLiteral(101), MakeLiteral(150));
            Assert.Equal(1, Math.Round(distance, 2));
        }

        [Fact]
        public void ComputeDistance_Null()
        {
            var distance = SyntaxComparer.ComputeDistance(
                null,
                Array.Empty<SyntaxNode>());

            Assert.Equal(0, Math.Round(distance, 2));

            distance = SyntaxComparer.ComputeDistance(
                Array.Empty<SyntaxNode>(),
                null);

            Assert.Equal(0, Math.Round(distance, 2));

            distance = SyntaxComparer.ComputeDistance(
                null,
                Array.Empty<SyntaxToken>());

            Assert.Equal(0, Math.Round(distance, 2));

            distance = SyntaxComparer.ComputeDistance(
                Array.Empty<SyntaxToken>(),
                null);

            Assert.Equal(0, Math.Round(distance, 2));
        }

        [Fact]
        public void ComputeDistance_LongSequences()
        {
            var t1 = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            var t2 = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
            var t3 = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);

            var distance = SyntaxComparer.ComputeDistance(
                Enumerable.Range(0, 10000).Select(i => i < 2000 ? t1 : t2),
                Enumerable.Range(0, 10000).Select(i => i < 2000 ? t1 : t3));

            // long sequences are indistinguishable if they have common prefix shorter then threshold:
            Assert.Equal(0, distance);
        }
    }
}
