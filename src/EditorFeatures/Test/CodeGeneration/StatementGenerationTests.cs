// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    public class StatementGenerationTests : AbstractCodeGenerationTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestThrowStatement1()
        {
            Test(f => f.ThrowStatement(),
                cs: "throw;",
                csSimple: null,
                vb: "Throw",
                vbSimple: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestThrowStatement2()
        {
            Test(f => f.ThrowStatement(
                f.IdentifierName("e")),
                cs: "throw e;",
                csSimple: null,
                vb: "Throw e",
                vbSimple: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestReturnStatement1()
        {
            Test(f => f.ReturnStatement(),
                cs: "return;",
                csSimple: null,
                vb: "Return",
                vbSimple: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestReturnStatement2()
        {
            Test(f => f.ReturnStatement(
                f.IdentifierName("e")),
                cs: "return e;",
                csSimple: null,
                vb: "Return e",
                vbSimple: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
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
