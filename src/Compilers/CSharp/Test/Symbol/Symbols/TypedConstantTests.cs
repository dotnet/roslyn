// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class TypedConstantTests : CSharpTestBase
    {
        private readonly CSharpCompilation _compilation;
        private readonly NamedTypeSymbol _namedType;
        private readonly NamedTypeSymbol _systemType;
        private readonly ArrayTypeSymbol _arrayType;
        private readonly TypeSymbol _intType;
        private readonly TypeSymbol _stringType;
        private readonly TypeSymbol _enumString1;
        private readonly TypeSymbol _enumString2;

        public TypedConstantTests()
        {
            _compilation = CreateCompilation("class C {}");
            _namedType = _compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            _systemType = _compilation.GetWellKnownType(WellKnownType.System_Type);
            _arrayType = _compilation.CreateArrayTypeSymbol(_compilation.GetSpecialType(SpecialType.System_Object));
            _intType = _compilation.GetSpecialType(SpecialType.System_Int32);
            _stringType = _compilation.GetSpecialType(SpecialType.System_String);
            _enumString1 = _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_compilation.GetSpecialType(SpecialType.System_String));
            _enumString2 = _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(_compilation.GetSpecialType(SpecialType.System_String));
        }

        [Fact]
        public void Conversions()
        {
            TypedConstant common = new TypedConstant(_systemType, TypedConstantKind.Type, _namedType);
            TypedConstant lang = (TypedConstant)common;
            TypedConstant common2 = lang;

            Assert.Equal(common.Value, lang.Value);
            Assert.Equal(common.Kind, lang.Kind);
            Assert.Equal<object>(common.Type, lang.Type);

            Assert.Equal(common.Value, common2.Value);
            Assert.Equal(common.Kind, common2.Kind);
            Assert.Equal(common.Type, common2.Type);

            TypedConstant commonArray = new TypedConstant(_arrayType,
                new[] { new TypedConstant(_systemType, TypedConstantKind.Type, _namedType) }.AsImmutableOrNull());

            TypedConstant langArray = (TypedConstant)commonArray;
            TypedConstant commonArray2 = langArray;

            Assert.Equal(commonArray.Values.Single(), langArray.Values.Single());
            Assert.Equal(commonArray.Kind, langArray.Kind);
            Assert.Equal<object>(commonArray.Type, langArray.Type);

            Assert.Equal(commonArray.Values, commonArray2.Values);
            Assert.Equal(commonArray.Kind, commonArray2.Kind);
            Assert.Equal(commonArray.Type, commonArray2.Type);
        }

        [Fact]
        public void Equality()
        {
            EqualityTesting.AssertEqual(default(TypedConstant), default(TypedConstant));

            EqualityTesting.AssertEqual(
                new TypedConstant(_intType, TypedConstantKind.Primitive, 1),
                new TypedConstant(_intType, TypedConstantKind.Primitive, 1));

            var s1 = "foo";
            var s2 = String.Format("{0}{1}{1}", "f", "o");

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
                new TypedConstant(_systemType, TypedConstantKind.Primitive, null));
        }
    }
}
