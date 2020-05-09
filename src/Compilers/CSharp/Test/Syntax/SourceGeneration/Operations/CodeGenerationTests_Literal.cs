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
        #region byte

        [Fact]
        public void TestLiteralSByteMinValue()
        {
            AssertEx.AreEqual(
@"global::System.SByte.MinValue",
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
@"global::System.SByte.MaxValue",
Literal(sbyte.MaxValue).GenerateString());
        }

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

        #endregion

        #region short

        [Fact]
        public void TestLiteralShortMinValue()
        {
            AssertEx.AreEqual(
@"global::System.SByte.MinValue",
Literal(sbyte.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralShortNegative1()
        {
            AssertEx.AreEqual(
@"-1",
Literal((short) -1).GenerateString());
        }

        [Fact]
        public void TestLiteralShortZero()
        {
            AssertEx.AreEqual(
@"0",
Literal((short)0).GenerateString());
        }

        [Fact]
        public void TestLiteralShortOne()
        {
            AssertEx.AreEqual(
@"1",
Literal((short)1).GenerateString());
        }

        [Fact]
        public void TestLiteralShortMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.Int16.MaxValue",
Literal(short.MaxValue).GenerateString());
        }

        [Fact]
        public void TestLiteralUShortMinValue()
        {
            AssertEx.AreEqual(
@"0",
Literal(ushort.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralUShortZero()
        {
            AssertEx.AreEqual(
@"0",
Literal((ushort)0).GenerateString());
        }

        [Fact]
        public void TestLiteralUShortOne()
        {
            AssertEx.AreEqual(
@"1",
Literal((ushort)1).GenerateString());
        }

        [Fact]
        public void TestLiteralUShortMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.UInt16.MaxValue",
Literal(ushort.MaxValue).GenerateString());
        }

        #endregion

        #region int

        [Fact]
        public void TestLiteralIntMinValue()
        {
            AssertEx.AreEqual(
@"global::System.Int32.MinValue",
Literal(int.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralIntNegative1()
        {
            AssertEx.AreEqual(
@"-1",
Literal((int)-1).GenerateString());
        }

        [Fact]
        public void TestLiteralIntZero()
        {
            AssertEx.AreEqual(
@"0",
Literal((int)0).GenerateString());
        }

        [Fact]
        public void TestLiteralIntOne()
        {
            AssertEx.AreEqual(
@"1",
Literal((int)1).GenerateString());
        }

        [Fact]
        public void TestLiteralIntMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.Int32.MaxValue",
Literal(int.MaxValue).GenerateString());
        }

        [Fact]
        public void TestLiteralUIntMinValue()
        {
            AssertEx.AreEqual(
@"0U",
Literal(uint.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralUIntZero()
        {
            AssertEx.AreEqual(
@"0U",
Literal((uint)0).GenerateString());
        }

        [Fact]
        public void TestLiteralUIntOne()
        {
            AssertEx.AreEqual(
@"1U",
Literal((uint)1).GenerateString());
        }

        [Fact]
        public void TestLiteralUIntMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.UInt32.MaxValue",
Literal(uint.MaxValue).GenerateString());
        }

        #endregion
    }
}
