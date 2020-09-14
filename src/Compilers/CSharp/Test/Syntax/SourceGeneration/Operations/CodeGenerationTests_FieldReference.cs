// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestStaticFieldReference()
        {
            AssertEx.AreEqual(
@"int.MaxValue",
FieldReference(Field(Int32, "MaxValue", modifiers: SymbolModifiers.Static), null).GenerateString());
        }

        [Fact]
        public void TestInstanceFieldReference()
        {
            AssertEx.AreEqual(
@"x.Length",
FieldReference(
    Field(System_Array, "Length"),
    LocalReference("x")).GenerateString());
        }

        [Fact]
        public void TestNullFieldReference()
        {
            AssertEx.AreEqual(
@"Length",
FieldReference(
    Field(null, "Length")).GenerateString());
        }
    }
}
