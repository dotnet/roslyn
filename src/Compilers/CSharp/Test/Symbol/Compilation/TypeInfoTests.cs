// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

            EqualityTesting.AssertEqual(new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), int32, nullable, notNullable),
                                        new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), int32, nullable, notNullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), nullable, nullable),
                                           new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), nullable, nullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), nullable, nullable),
                                           new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), nullable, nullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), nullable, nullable),
                                           new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.NotAnnotated), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), notNullable, nullable));

            EqualityTesting.AssertNotEqual(new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), nullable, nullable),
                                           new TypeInfo(obj.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.Annotated), int32, nullable, notNullable));

            EqualityTesting.AssertEqual(new TypeInfo(int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), default, default),
                                        new TypeInfo(int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), default, default));

            var intEnum1 = c.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).GetPublicSymbol().Construct(int32);
            var intEnum2 = c.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).GetPublicSymbol().Construct(int32);
            EqualityTesting.AssertEqual(new TypeInfo(intEnum1.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), default, default),
                new TypeInfo(intEnum2.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), int32.WithNullableAnnotation(CodeAnalysis.NullableAnnotation.None), default, default));
        }
    }
}
