// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        public void TestLiteralSByteNegativeOne()
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
        public void TestLiteralShortNegativeOne()
        {
            AssertEx.AreEqual(
@"-1",
Literal((short)-1).GenerateString());
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
        public void TestLiteralIntNegativeOne()
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

        #region long

        [Fact]
        public void TestLiteralLongMinValue()
        {
            AssertEx.AreEqual(
@"global::System.Int64.MinValue",
Literal(long.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralLongNegativeOne()
        {
            AssertEx.AreEqual(
@"-1L",
Literal((long)-1).GenerateString());
        }

        [Fact]
        public void TestLiteralLongZero()
        {
            AssertEx.AreEqual(
@"0L",
Literal((long)0).GenerateString());
        }

        [Fact]
        public void TestLiteralLongOne()
        {
            AssertEx.AreEqual(
@"1L",
Literal((long)1).GenerateString());
        }

        [Fact]
        public void TestLiteralLongMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.Int64.MaxValue",
Literal(long.MaxValue).GenerateString());
        }

        [Fact]
        public void TestLiteralULongMinValue()
        {
            AssertEx.AreEqual(
@"0UL",
Literal(ulong.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralULongZero()
        {
            AssertEx.AreEqual(
@"0UL",
Literal((ulong)0).GenerateString());
        }

        [Fact]
        public void TestLiteralULongOne()
        {
            AssertEx.AreEqual(
@"1UL",
Literal((ulong)1).GenerateString());
        }

        [Fact]
        public void TestLiteralULongMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.UInt64.MaxValue",
Literal(ulong.MaxValue).GenerateString());
        }

        #endregion

        #region long

        [Fact]
        public void TestLiteralSingleMinValue()
        {
            AssertEx.AreEqual(
@"global::System.Single.MinValue",
Literal(float.MinValue).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleNegativeOne()
        {
            AssertEx.AreEqual(
@"-1F",
Literal((float)-1).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleNegativeOneHalf()
        {
            AssertEx.AreEqual(
@"-0.5F",
Literal((float)-1 / 2).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleZero()
        {
            AssertEx.AreEqual(
@"0F",
Literal((float)0).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleOne()
        {
            AssertEx.AreEqual(
@"1F",
Literal((float)1).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleOneHalf()
        {
            AssertEx.AreEqual(
@"0.5F",
Literal((float)0.5).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleMaxValue()
        {
            AssertEx.AreEqual(
@"global::System.Single.MaxValue",
Literal(float.MaxValue).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleNaNValue()
        {
            AssertEx.AreEqual(
@"global::System.Single.NaN",
Literal(float.NaN).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleEpsilonValue()
        {
            AssertEx.AreEqual(
@"global::System.Single.Epsilon",
Literal(float.Epsilon).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleNegativeInfinityValue()
        {
            AssertEx.AreEqual(
@"global::System.Single.NegativeInfinity",
Literal(float.NegativeInfinity).GenerateString());
        }

        [Fact]
        public void TestLiteralSinglePositiveInfinityValue()
        {
            AssertEx.AreEqual(
@"global::System.Single.PositiveInfinity",
Literal(float.PositiveInfinity).GenerateString());
        }

        [Fact]
        public void TestLiteralSingleMathPi()
        {
            AssertEx.AreEqual(
@"3.1415927F",
Literal((float)Math.PI).GenerateString());
        }

        #endregion
    }
}
