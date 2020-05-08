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
        public void TestTupleWithoutFieldNames()
        {
            AssertEx.AreEqual(
"(int, bool)",
TupleType(ImmutableArray.Create(
    TupleElement(Int32),
    TupleElement(Boolean))).GenerateTypeString());
        }

        [Fact]
        public void TestTupleWithFirstFieldName()
        {
            AssertEx.AreEqual(
"(int a, bool)",
TupleType(ImmutableArray.Create(
    TupleElement(Int32, "a"),
    TupleElement(Boolean))).GenerateTypeString());
        }

        [Fact]
        public void TestTupleWithSecondFieldName()
        {
            AssertEx.AreEqual(
"(int, bool b)",
TupleType(ImmutableArray.Create(
    TupleElement(Int32),
    TupleElement(Boolean, "b"))).GenerateTypeString());
        }

        [Fact]
        public void TestTupleWithFieldNames()
        {
            AssertEx.AreEqual(
"(int a, bool b)",
TupleType(ImmutableArray.Create(
    TupleElement(Int32, "a"),
    TupleElement(Boolean, "b"))).GenerateTypeString());
        }
    }
}
