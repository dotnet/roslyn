// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.IncrementalParsing
{
    // These tests handle changing between asynchronous and synchronous parsing contexts as the 'async' modifier is added / removed.
    public class ChangingAsync
    {
        [Fact]
        public void AddAsync()
        {
            string oldText =
@"class Test
{
    public static void F()
    {
        await t;
    }
}";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithInsertBefore("public", "async ");

                Assert.Equal(default(SyntaxNodeOrToken), oldTree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression));
                Assert.NotEqual(default(SyntaxNodeOrToken), newTree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression));
            });
        }

        [Fact]
        public void RemoveAsync()
        {
            string oldText =
@"class Test
{
    async public static void F()
    {
        await t;
    }
}";

            ParseAndVerify(oldText, validator: oldTree =>
            {
                var newTree = oldTree.WithRemoveFirst("async");

                Assert.NotEqual(default(SyntaxNodeOrToken), oldTree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression));
                Assert.Equal(default(SyntaxNodeOrToken), newTree.FindNodeOrTokenByKind(SyntaxKind.AwaitExpression));
            });
        }

        #region Helpers
        private static void ParseAndVerify(string text, Action<SyntaxTree> validator)
        {
            ParseAndValidate(text, validator, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5));
            ParseAndValidate(text, validator, TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        private static void ParseAndValidate(string text, Action<SyntaxTree> validator, CSharpParseOptions options = null)
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree(text);
            validator(oldTree);
        }
        #endregion
    }
}
