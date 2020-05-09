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
        public void TestLiteralByteMinValue()
        {
            AssertEx.AreEqual(
@"0",
Literal(byte.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralByteZero()
        {
            AssertEx.AreEqual(
@"0",
Literal((byte)0).GenerateString());
        }

        [Fact]
        public void TestLiteralByteOne()
        {
            AssertEx.AreEqual(
@"1",
Literal((byte)1).GenerateString());
        }

        [Fact]
        public void TestLiteralByteMaxValue()
        {
            AssertEx.AreEqual(
@"255",
Literal(byte.MaxValue).GenerateString());
        }

        [Fact]
        public void TestLiteralSByteMinValue()
        {
            AssertEx.AreEqual(
@"-128",
Literal(sbyte.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralSByteNegative1()
        {
            AssertEx.AreEqual(
@"-1",
Literal((sbyte)-1).GenerateString());
        }

        [Fact]
        public void TestLiteralSByteZero()
        {
            AssertEx.AreEqual(
@"0",
Literal((sbyte)0).GenerateString());
        }

        [Fact]
        public void TestLiteralSByteOne()
        {
            AssertEx.AreEqual(
@"1",
Literal((sbyte)1).GenerateString());
        }

        [Fact]
        public void TestLiteralSByteMaxValue()
        {
            AssertEx.AreEqual(
@"127",
Literal(sbyte.MaxValue).GenerateString());
        }
    }
}
