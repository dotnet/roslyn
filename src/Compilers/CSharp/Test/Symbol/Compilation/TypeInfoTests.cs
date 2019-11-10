// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class TypeInfoTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var c = CreateCompilation("");
            var obj = c.GetSpecialType(SpecialType.System_Object).GetPublicSymbol();
            var int32 = c.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var notNullable = new NullabilityInfo(CodeAnalysis.NullableAnnotation.NotAnnotated, CodeAnalysis.NullableFlowState.NotNull);
            var nullable = new NullabilityInfo(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableFlowState.MaybeNull);

            EqualityTesting.AssertEqual(default(TypeInfo), default(TypeInfo));

            EqualityTesting.AssertEqual(new TypeInfo(obj, int32, nullable, notNullable),
                                        new TypeInfo(obj, int32, nullable, notNullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(obj, obj, nullable, nullable),
                                           new TypeInfo(obj, int32, nullable, nullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(int32, obj, nullable, nullable),
                                           new TypeInfo(obj, obj, nullable, nullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(obj, int32, nullable, nullable),
                                           new TypeInfo(obj, int32, notNullable, nullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(obj, int32, nullable, nullable),
                                           new TypeInfo(obj, int32, nullable, notNullable));

            EqualityTesting.AssertEqual(new TypeInfo(int32, int32, default, default),
                                        new TypeInfo(int32, int32, default, default));

            var intEnum1 = c.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).GetPublicSymbol().Construct(int32);
            var intEnum2 = c.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).GetPublicSymbol().Construct(int32);
            EqualityTesting.AssertEqual(new TypeInfo(intEnum1, int32, default, default),
                new TypeInfo(intEnum2, int32, default, default));
        }
    }
}
