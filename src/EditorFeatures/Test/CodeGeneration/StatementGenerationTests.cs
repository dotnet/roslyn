// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    [Trait(Traits.Feature, Traits.Features.CodeGeneration)]
    public class StatementGenerationTests : AbstractCodeGenerationTests
    {
        [Fact]
        public void TestThrowStatement1()
        {
            Test(f => f.ThrowStatement(),
                cs: "throw;",
                csSimple: null,
                vb: "Throw",
                vbSimple: null);
        }

        [Fact]
        public void TestThrowStatement2()
        {
            Test(f => f.ThrowStatement(
                f.IdentifierName("e")),
                cs: "throw e;",
                csSimple: null,
                vb: "Throw e",
                vbSimple: null);
        }

        [Fact]
        public void TestThrowStatement3()
        {
            Test(f => f.ThrowStatement(
                f.ObjectCreationExpression(
                    CreateClass("NotImplementedException"))),
                cs: "throw new NotImplementedException();",
                csSimple: null,
                vb: "Throw New NotImplementedException()",
                vbSimple: null);
        }

        [Fact]
        public void TestReturnStatement1()
        {
            Test(f => f.ReturnStatement(),
                cs: "return;",
                csSimple: null,
                vb: "Return",
                vbSimple: null);
        }

        [Fact]
        public void TestReturnStatement2()
        {
            Test(f => f.ReturnStatement(
                f.IdentifierName("e")),
                cs: "return e;",
                csSimple: null,
                vb: "Return e",
                vbSimple: null);
        }

        [Fact]
        public void TestReturnStatement3()
        {
            Test(f => f.ReturnStatement(
                f.ObjectCreationExpression(
                    CreateClass("NotImplementedException"))),
                cs: "return new NotImplementedException();",
                csSimple: null,
                vb: "Return New NotImplementedException()",
                vbSimple: null);
        }
    }
}
