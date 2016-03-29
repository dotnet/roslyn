// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ReplaceOriginalTests
    {
        [Fact(Skip = "PROTOTYPE(generators): Incremental parsing")]
        public void AddReplace()
        {
            string oldText =
@"class C
{
    static void F()
    {
        original();
    }
}";
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText);
            var newTree = oldTree.WithInsertBefore("static", "replace ");
            Assert.Equal(default(SyntaxNodeOrToken), oldTree.FindNodeOrTokenByKind(SyntaxKind.OriginalExpression));
            Assert.NotEqual(default(SyntaxNodeOrToken), newTree.FindNodeOrTokenByKind(SyntaxKind.OriginalExpression));
        }

        [Fact(Skip = "PROTOTYPE(generators): Incremental parsing")]
        public void RemoveReplace()
        {
            string oldText =
@"class C
{
    replace static void F()
    {
        original();
    }
}";
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText);
            var newTree = oldTree.WithRemoveFirst("replace");
            Assert.NotEqual(default(SyntaxNodeOrToken), oldTree.FindNodeOrTokenByKind(SyntaxKind.OriginalExpression));
            Assert.Equal(default(SyntaxNodeOrToken), newTree.FindNodeOrTokenByKind(SyntaxKind.OriginalExpression));
        }
    }
}
