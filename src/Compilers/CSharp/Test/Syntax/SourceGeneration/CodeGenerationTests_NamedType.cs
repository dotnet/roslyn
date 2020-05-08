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
        public void TestClassType1()
        {
            AssertEx.AreEqual(
"x",
Class("x").GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeWithTypeArguments1()
        {
            AssertEx.AreEqual(
"X<int>",
Class(
    "X",
    typeArguments: ImmutableArray.Create(Int32)).GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeWithTypeArguments2()
        {
            AssertEx.AreEqual(
"X<int, bool>",
Class(
    "X",
    typeArguments: ImmutableArray.Create(Int32, Boolean)).GenerateTypeString());
        }
    }
}
