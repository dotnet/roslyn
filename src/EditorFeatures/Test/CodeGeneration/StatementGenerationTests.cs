﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                vb: "Throw");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestThrowStatement2()
        {
            Test(f => f.ThrowStatement(
                f.IdentifierName("e")),
                cs: "throw e;",
                vb: "Throw e");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestThrowStatement3()
        {
            Test(f => f.ThrowStatement(
                f.ObjectCreationExpression(
                    CreateClass("NotImplementedException"))),
                cs: "throw new NotImplementedException();",
                vb: "Throw New NotImplementedException()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestReturnStatement1()
        {
            Test(f => f.ReturnStatement(),
                cs: "return;",
                vb: "Return");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestReturnStatement2()
        {
            Test(f => f.ReturnStatement(
                f.IdentifierName("e")),
                cs: "return e;",
                vb: "Return e");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestReturnStatement3()
        {
            Test(f => f.ReturnStatement(
                f.ObjectCreationExpression(
                    CreateClass("NotImplementedException"))),
                cs: "return new NotImplementedException();",
                vb: "Return New NotImplementedException()");
        }
    }
}
