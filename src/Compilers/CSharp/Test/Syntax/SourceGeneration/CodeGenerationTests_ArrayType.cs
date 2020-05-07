// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestArrayOfInt32()
        {
            AssertEx.AreEqual(
"int[]",
ArrayType(SpecialType(SpecialType.System_Int32)).GenerateTypeString());
        }

        [Fact]
        public void TestArrayOfInt32WithRank2()
        {
            AssertEx.AreEqual(
"int[, ]",
ArrayType(SpecialType(SpecialType.System_Int32), rank: 2).GenerateTypeString());
        }

        [Fact]
        public void TestNullableArrayOfInt32()
        {
            AssertEx.AreEqual(
"int[]?",
ArrayType(SpecialType(SpecialType.System_Int32), nullableAnnotation: CodeAnalysis.NullableAnnotation.Annotated).GenerateTypeString());
        }

        [Fact]
        public void TestNullableArrayOfInt32WithRank2()
        {
            AssertEx.AreEqual(
"int[, ]?",
ArrayType(SpecialType(SpecialType.System_Int32), rank: 2, nullableAnnotation: CodeAnalysis.NullableAnnotation.Annotated).GenerateTypeString());
        }
    }
}
