// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CommonTypedConstantTests : TestBase
    {
        private readonly CSharp.CSharpCompilation compilation;
        private readonly ITypeSymbol intType;
        private readonly ITypeSymbol stringType;
        private readonly ITypeSymbol enumString1;
        private readonly ITypeSymbol enumString2;

        public CommonTypedConstantTests()
        {
            compilation = (CSharp.CSharpCompilation)CSharp.CSharpCompilation.Create("class C {}");
            intType = compilation.GetSpecialType(SpecialType.System_Int32);
            stringType = compilation.GetSpecialType(SpecialType.System_String);
            enumString1 = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(compilation.GetSpecialType(SpecialType.System_String));
            enumString2 = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(compilation.GetSpecialType(SpecialType.System_String));
        }

        [Fact]
        public void Equality()
        {
            EqualityTesting.AssertEqual(default(TypedConstant), default(TypedConstant));

            EqualityTesting.AssertEqual(
                new TypedConstant(this.intType, TypedConstantKind.Primitive, 1),
                new TypedConstant(this.intType, TypedConstantKind.Primitive, 1));

            var s1 = "foo";
            var s2 = String.Format("{0}{1}{1}", "f", "o");

            EqualityTesting.AssertEqual(
                new TypedConstant(this.stringType, TypedConstantKind.Primitive, s1),
                new TypedConstant(this.stringType, TypedConstantKind.Primitive, s2));

            EqualityTesting.AssertEqual(
                new TypedConstant(this.stringType, TypedConstantKind.Primitive, null),
                new TypedConstant(this.stringType, TypedConstantKind.Primitive, null));

            EqualityTesting.AssertEqual(
                new TypedConstant(this.enumString1, TypedConstantKind.Primitive, null),
                new TypedConstant(this.enumString2, TypedConstantKind.Primitive, null));

            EqualityTesting.AssertNotEqual(
                new TypedConstant(this.stringType, TypedConstantKind.Primitive, null),
                new TypedConstant(this.stringType, TypedConstantKind.Error, null));

            EqualityTesting.AssertNotEqual(
                new TypedConstant(this.stringType, TypedConstantKind.Primitive, null),
                new TypedConstant(this.enumString1, TypedConstantKind.Primitive, null));
        }
    }
}
