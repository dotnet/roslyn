// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var obj = c.GetSpecialType(SpecialType.System_Object);
            var int32 = c.GetSpecialType(SpecialType.System_Int32);

            EqualityTesting.AssertEqual(default(TypeInfo), default(TypeInfo));
            EqualityTesting.AssertEqual(new TypeInfo(obj, int32), new TypeInfo(obj, int32));
            EqualityTesting.AssertNotEqual(new TypeInfo(obj, obj), new TypeInfo(obj, int32));
            EqualityTesting.AssertNotEqual(new TypeInfo(int32, obj), new TypeInfo(obj, obj));
            EqualityTesting.AssertEqual(new TypeInfo(int32, int32), new TypeInfo(int32, int32));

            var intEnum1 = c.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(int32);
            var intEnum2 = c.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(int32);
            EqualityTesting.AssertEqual(new TypeInfo(intEnum1, int32), new TypeInfo(intEnum2, int32));
        }
    }
}
