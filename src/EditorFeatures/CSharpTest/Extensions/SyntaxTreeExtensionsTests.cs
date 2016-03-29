// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Extensions
{
    public class SyntaxTreeExtensionsTests
    {
        private static void VerifyWholeLineIsActive(SyntaxTree tree, int lineNumber)
        {
            var line = tree.GetText().Lines[lineNumber];
            for (int pos = line.Start; pos < line.EndIncludingLineBreak; pos++)
            {
                Assert.False(tree.IsInInactiveRegion(pos, CancellationToken.None));
            }
        }

        private static void VerifyWholeLineIsInactive(SyntaxTree tree, int lineNumber)
        {
            var line = tree.GetText().Lines[lineNumber];
            for (int pos = line.Start; pos < line.EndIncludingLineBreak; pos++)
            {
                Assert.True(tree.IsInInactiveRegion(pos, CancellationToken.None));
            }
        }

        [Fact]
        public void SimpleInactive()
        {
            var code = @"#if false
This is inactive
#else
// This is active
#endif
";
            var tree = CSharpSyntaxTree.ParseText(code);
            VerifyWholeLineIsActive(tree, 0);
            VerifyWholeLineIsInactive(tree, 1);
            VerifyWholeLineIsActive(tree, 2);
            VerifyWholeLineIsActive(tree, 3);
            VerifyWholeLineIsActive(tree, 4);
        }

        [Fact]
        public void InactiveEof()
        {
            var code = @"#if false
This is inactive
";
            var tree = CSharpSyntaxTree.ParseText(code);
            VerifyWholeLineIsActive(tree, 0);
            VerifyWholeLineIsInactive(tree, 1);
        }

        [Fact]
        public void InactiveEof2()
        {
            var code = @"#if false
This is inactive
#endif
// This is active
";

            var tree = CSharpSyntaxTree.ParseText(code);
            VerifyWholeLineIsActive(tree, 0);
            VerifyWholeLineIsInactive(tree, 1);
            VerifyWholeLineIsActive(tree, 2);
            VerifyWholeLineIsActive(tree, 3);
        }
    }
}
