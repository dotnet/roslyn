// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestReturn1()
        {
            AssertEx.AreEqual(
@"return;",
Return().GenerateString());
        }

        [Fact]
        public void TestYieldBreak1()
        {
            AssertEx.AreEqual(
@"yield break;",
YieldBreak().GenerateString());
        }

        [Fact]
        public void TestYieldReturn1()
        {
            AssertEx.AreEqual(
@"yield return 0;",
YieldReturn(Literal(0)).GenerateString());
        }
    }
}
