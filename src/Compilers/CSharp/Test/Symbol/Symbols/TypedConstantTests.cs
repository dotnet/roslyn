// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly CSharpCompilation compilation;
        private readonly NamedTypeSymbol namedType;
        private readonly NamedTypeSymbol systemType;
        private readonly ArrayTypeSymbol arrayType;
        private readonly TypeSymbol intType;
        private readonly TypeSymbol stringType;
        private readonly TypeSymbol enumString1;
        private readonly TypeSymbol enumString2;

        public TypedConstantTests()
        {
            compilation = CreateCompilationWithMscorlib("class C {}");
            namedType = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            systemType = compilation.GetWellKnownType(WellKnownType.System_Type);
            arrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object));
            intType = compilation.GetSpecialType(SpecialType.System_Int32);
            stringType = compilation.GetSpecialType(SpecialType.System_String);
            enumString1 = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(compilation.GetSpecialType(SpecialType.System_String));
            enumString2 = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(compilation.GetSpecialType(SpecialType.System_String));
        }

        [Fact]
        public void Conversions()
        {
            TypedConstant common = new TypedConstant(systemType, TypedConstantKind.Type, namedType);
            TypedConstant lang = (TypedConstant)common;
            TypedConstant common2 = lang;

            Assert.Equal(common.Value, lang.Value);
            Assert.Equal(common.Kind, lang.Kind);
            Assert.Equal<object>(common.Type, lang.Type);

            Assert.Equal(common.Value, common2.Value);
            Assert.Equal(common.Kind, common2.Kind);
            Assert.Equal(common.Type, common2.Type);

            TypedConstant commonArray = new TypedConstant(arrayType, 
                new[] { new TypedConstant(systemType, TypedConstantKind.Type, namedType) }.AsImmutableOrNull());

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
                new TypedConstant(this.systemType, TypedConstantKind.Primitive, null));

        }
    }
}
