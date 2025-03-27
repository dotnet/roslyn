// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Symbols;
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
        private readonly CSharp.CSharpCompilation _compilation;
        private readonly ITypeSymbolInternal _intType;
        private readonly ITypeSymbolInternal _stringType;
        private readonly ITypeSymbolInternal _enumString1;
        private readonly ITypeSymbolInternal _enumString2;

        public CommonTypedConstantTests()
        {
            _compilation = (CSharp.CSharpCompilation)CSharp.CSharpCompilation.Create("class C {}");
            _intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            _stringType = _compilation.GetSpecialType(SpecialType.System_String);
            _enumString1 = _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_compilation.GetSpecialType(SpecialType.System_String));
            _enumString2 = _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_compilation.GetSpecialType(SpecialType.System_String));
        }

        [Fact]
        public void Equality()
        {
            EqualityTesting.AssertEqual(default(TypedConstant), default(TypedConstant));

            EqualityTesting.AssertEqual(
                new TypedConstant(_intType, TypedConstantKind.Primitive, 1),
                new TypedConstant(_intType, TypedConstantKind.Primitive, 1));

            var s1 = "goo";
            var s2 = String.Format("{0}{1}{1}", "g", "o");

            EqualityTesting.AssertEqual(
                new TypedConstant(_stringType, TypedConstantKind.Primitive, s1),
                new TypedConstant(_stringType, TypedConstantKind.Primitive, s2));

            EqualityTesting.AssertEqual(
                new TypedConstant(_stringType, TypedConstantKind.Primitive, null),
                new TypedConstant(_stringType, TypedConstantKind.Primitive, null));

            EqualityTesting.AssertEqual(
                new TypedConstant(_enumString1, TypedConstantKind.Primitive, null),
                new TypedConstant(_enumString2, TypedConstantKind.Primitive, null));

            EqualityTesting.AssertNotEqual(
                new TypedConstant(_stringType, TypedConstantKind.Primitive, null),
                new TypedConstant(_stringType, TypedConstantKind.Error, null));

            EqualityTesting.AssertNotEqual(
                new TypedConstant(_stringType, TypedConstantKind.Primitive, null),
                new TypedConstant(_enumString1, TypedConstantKind.Primitive, null));
        }
    }
}
