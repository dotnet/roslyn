// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ReplaceOriginalTests
    {
        [WorkItem(11121, "https://github.com/dotnet/roslyn/issues/11121")]
        [Fact(Skip = "11121")]
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

        [WorkItem(11121, "https://github.com/dotnet/roslyn/issues/11121")]
        [Fact(Skip = "11121")]
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
