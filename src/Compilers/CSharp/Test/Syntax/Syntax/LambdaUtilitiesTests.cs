// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaUtilitiesTests
    {
        [Fact]
        public void AreEquivalentIgnoringLambdaBodies1()
        {
            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(1)"),
                SyntaxFactory.ParseExpression("F(1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(1)"),
                SyntaxFactory.ParseExpression("F(2)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(a => 1)"),
                SyntaxFactory.ParseExpression("F(a => 2)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(() => 1)"),
                SyntaxFactory.ParseExpression("F(() => 2)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(delegate { return 1; })"),
                SyntaxFactory.ParseExpression("F(delegate { return 2; })")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(delegate (int a) { return 1; })"),
                SyntaxFactory.ParseExpression("F(delegate (bool a) { return 1; })")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(delegate (int a) { return 1; })"),
                SyntaxFactory.ParseExpression("F(delegate (int a) { return 2; })")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(() => { return 1; })"),
                SyntaxFactory.ParseExpression("F(() => { return 1; })")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(() => { return 1; })"),
                SyntaxFactory.ParseExpression("F((a) => { return 1; })")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a + 1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } where b > 0 select a)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } from b in new[] { 3, 4 } where b > 0 select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } from b in new[] { 3, 4, 5 } where b > 1 select a + 1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a equals b select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4, 5 } on a equals b select a)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a equals b select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a + 1 equals b + 1 select a)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a equals b select a)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into g select g)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a + 1 by a + 2 into g select g)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into g select g)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into q select q)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a, a descending, a ascending select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a + 1, a - 1 descending, a + 1 ascending select a)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a, a descending, a ascending select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a, a descending, a descending select a)")));
        }
    }
}
