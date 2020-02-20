﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    using static Microsoft.CodeAnalysis.CSharp.ValueSetFactory;
    using static BinaryOperatorKind;

    /// <summary>
    /// Test some internal implementation data structures used in <see cref="DecisionDagBuilder"/>.
    /// </summary>
    public class ValueSetTests
    {
        private static Random Random = new Random();

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-3)]
        [InlineData(-4)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void TestGE_01(int i1)
        {
            IValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1);
            Assert.Equal($"[{i1}..{int.MaxValue}]", values.ToString());
        }

        [Fact]
        public void TestGE_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                IValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1);
                Assert.Equal($"[{i1}..{int.MaxValue}]", values.ToString());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-3)]
        [InlineData(-4)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void TestGT_01(int i1)
        {
            IValueSet<int> values = ForInt.Related(GreaterThan, i1);
            Assert.Equal((i1 == int.MaxValue) ? "" : $"[{i1 + 1}..{int.MaxValue}]", values.ToString());
        }

        [Fact]
        public void TestGT_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                IValueSet<int> values = ForInt.Related(GreaterThan, i1);
                Assert.Equal($"[{i1 + 1}..{int.MaxValue}]", values.ToString());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-3)]
        [InlineData(-4)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void TestLE_01(int i1)
        {
            IValueSet<int> values = ForInt.Related(LessThanOrEqual, i1);
            Assert.Equal($"[{int.MinValue}..{i1}]", values.ToString());
        }

        [Fact]
        public void TestLE_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue) + 1;
                IValueSet<int> values = ForInt.Related(LessThanOrEqual, i1);
                Assert.Equal($"[{int.MinValue}..{i1}]", values.ToString());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-3)]
        [InlineData(-4)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void TestLT_01(int i1)
        {
            IValueSet<int> values = ForInt.Related(LessThan, i1);
            Assert.Equal((i1 == int.MinValue) ? "" : $"[{int.MinValue}..{i1 - 1}]", values.ToString());
        }

        [Fact]
        public void TestLT_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue) + 1;
                IValueSet<int> values = ForInt.Related(LessThan, i1);
                Assert.Equal($"[{int.MinValue}..{i1 - 1}]", values.ToString());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-3)]
        [InlineData(-4)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void TestEQ_01(int i1)
        {
            IValueSet<int> values = ForInt.Related(Equal, i1);
            Assert.Equal($"[{i1}..{i1}]", values.ToString());
        }

        [Fact]
        public void TestEQ_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                IValueSet<int> values = ForInt.Related(Equal, i1);
                Assert.Equal($"[{i1}..{i1}]", values.ToString());
            }
        }

        [Fact]
        public void TestIntersect_01()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue + 1, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                if (i1 > i2) (i1, i2) = (i2, i1);
                IValueSet<int> values1 = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal($"[{i1}..{i2}]", values1.ToString());
                IValueSet<int> values2 = ForInt.Related(LessThanOrEqual, i2).Intersect(ForInt.Related(GreaterThanOrEqual, i1));
                Assert.Equal(values1, values2);
            }
        }

        [Fact]
        public void TestIntersect_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue + 1, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                if (i1 < i2) (i1, i2) = (i2, i1);
                if (i1 == i2) continue;
                IValueSet<int> values1 = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal($"", values1.ToString());
                IValueSet<int> values2 = ForInt.Related(LessThanOrEqual, i2).Intersect(ForInt.Related(GreaterThanOrEqual, i1));
                Assert.Equal(values1, values2);
            }
        }

        [Fact]
        public void TestUnion_01()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue + 1, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                if (i1 > i2) (i1, i2) = (i2, i1);
                if ((i1 + 1) >= i2) continue;
                IValueSet<int> values1 = ForInt.Related(LessThanOrEqual, i1).Union(ForInt.Related(GreaterThanOrEqual, i2));
                Assert.Equal($"[{int.MinValue}..{i1}],[{i2}..{int.MaxValue}]", values1.ToString());
                IValueSet<int> values2 = ForInt.Related(GreaterThanOrEqual, i2).Union(ForInt.Related(LessThanOrEqual, i1));
                Assert.Equal(values1, values2);
            }
        }

        [Fact]
        public void TestUnion_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue + 1, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                if (i1 < i2) (i1, i2) = (i2, i1);
                IValueSet<int> values1 = ForInt.Related(LessThanOrEqual, i1).Union(ForInt.Related(GreaterThanOrEqual, i2));
                Assert.Equal($"[{int.MinValue}..{int.MaxValue}]", values1.ToString());
                IValueSet<int> values2 = ForInt.Related(GreaterThanOrEqual, i2).Union(ForInt.Related(LessThanOrEqual, i1));
                Assert.Equal(values1, values2);
            }
        }

        [Fact]
        public void TestComplement_01()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue + 1, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                if (i1 > i2) (i1, i2) = (i2, i1);
                if ((i1 + 1) >= i2) continue;
                IValueSet<int> values1 = ForInt.Related(LessThanOrEqual, i1).Union(ForInt.Related(GreaterThanOrEqual, i2));
                Assert.Equal($"[{int.MinValue}..{i1}],[{i2}..{int.MaxValue}]", values1.ToString());
                IValueSet<int> values2 = values1.Complement();
                Assert.Equal($"[{i1 + 1}..{i2 - 1}]", values2.ToString());
            }
        }

        [Fact]
        public void TestAny_01()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                if (i1 > i2) (i1, i2) = (i2, i1);
                IValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal($"[{i1}..{i2}]", values.ToString());
                test(int.MinValue);
                if (i1 != int.MinValue) test(i1 - 1);
                test(i1);
                test(i1 + 1);
                test(int.MaxValue);
                if (i2 != int.MinValue) test(i2 - 1);
                test(i2);
                test(i2 + 1);
                test(Random.Next(int.MinValue, int.MaxValue));
                test(Random.Next(int.MinValue, int.MaxValue));
                void test(int val)
                {
                    Assert.Equal(val >= i1 && val <= i2, values.Any(Equal, val));
                    Assert.Equal(val >= i1, values.Any(LessThanOrEqual, val));
                    Assert.Equal(val > i1, values.Any(LessThan, val));
                    Assert.Equal(val <= i2, values.Any(GreaterThanOrEqual, val));
                    Assert.Equal(i2 > val, values.Any(GreaterThan, val));
                }
            }
        }

        [Fact]
        public void TestIsEmpty_01()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                int i2 = Random.Next(int.MinValue, int.MaxValue);
                IValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal(values.ToString().Length == 0, values.IsEmpty);
            }
        }

        [Fact]
        public void TestDouble_01()
        {
            for (int i = 0; i < 100; i++)
            {
                double d1 = Random.NextDouble() * 100 - 50;
                double d2 = Random.NextDouble() * 100 - 50;
                if (d1 > d2) (d1, d2) = (d2, d1);
                IValueSet<double> values = ForDouble.Related(GreaterThanOrEqual, d1).Intersect(ForDouble.Related(LessThanOrEqual, d2));
                Assert.Equal(FormattableString.Invariant($"[{d1:G17}..{d2:G17}]"), values.ToString());
            }
        }

        [Fact]
        public void TestChar_01()
        {
            IValueSet<char> gea1 = ForChar.Related(GreaterThanOrEqual, 'a');
            IValueSet<char> lez1 = ForChar.Related(LessThanOrEqual, 'z');
            IValueSet<char> gea2 = ForChar.Related(GreaterThanOrEqual, 'A');
            IValueSet<char> lez2 = ForChar.Related(LessThanOrEqual, 'Z');
            var letters = gea1.Intersect(lez1).Union(gea2.Intersect(lez2));
            Assert.Equal("['A'..'Z'],['a'..'z']", letters.ToString());
        }

        [Fact]
        public void TestDouble_02()
        {
            Assert.Equal("-Inf", ForDouble.Related(LessThan, double.MinValue).ToString());
            var lt = ForDouble.Related(LessThan, 0.0);
            Assert.Equal(FormattableString.Invariant($"-Inf,[{double.MinValue:G17}..{-double.Epsilon:G17}]"), lt.ToString());
            var gt = ForDouble.Related(GreaterThan, 0.0);
            Assert.Equal(FormattableString.Invariant($"Inf,[{double.Epsilon:G17}..{double.MaxValue:G17}]"), gt.ToString());
            var eq = ForDouble.Related(Equal, 0.0);
            Assert.Equal("[0..0]", eq.ToString());
            var none = lt.Complement().Intersect(gt.Complement()).Intersect(eq.Complement());
            Assert.Equal("NaN", none.ToString());
            Assert.False(none.IsEmpty);
        }

        [Fact]
        public void TestFloat_01()
        {
            Assert.Equal("-Inf", ForFloat.Related(LessThan, float.MinValue).ToString());
            var lt = ForFloat.Related(LessThan, 0.0f);
            Assert.Equal(FormattableString.Invariant($"-Inf,[{float.MinValue:G9}..{-float.Epsilon:G9}]"), lt.ToString());
            var gt = ForFloat.Related(GreaterThan, 0.0f);
            Assert.Equal(FormattableString.Invariant($"Inf,[{float.Epsilon:G9}..{float.MaxValue:G9}]"), gt.ToString());
            var eq = ForFloat.Related(Equal, 0.0f);
            Assert.Equal("[0..0]", eq.ToString());
            var none = lt.Complement().Intersect(gt.Complement()).Intersect(eq.Complement());
            Assert.Equal("NaN", none.ToString());
            Assert.False(none.IsEmpty);
        }

        [Fact]
        public void TestDouble_03()
        {
            Assert.Equal("NaN", ForDouble.Related(Equal, double.NaN).ToString());
            Assert.Equal("NaN", ForFloat.Related(Equal, float.NaN).ToString());
            Assert.True(ForDouble.Related(Equal, double.NaN).Any(Equal, double.NaN));
            Assert.True(ForFloat.Related(Equal, float.NaN).Any(Equal, float.NaN));
            Assert.Equal("Inf", ForDouble.Related(Equal, double.PositiveInfinity).ToString());
            Assert.Equal("Inf", ForFloat.Related(Equal, float.PositiveInfinity).ToString());
            Assert.Equal("-Inf", ForDouble.Related(Equal, double.NegativeInfinity).ToString());
            Assert.Equal("-Inf", ForFloat.Related(Equal, float.NegativeInfinity).ToString());
        }

        [Fact]
        public void TestDouble_04()
        {
            var neg = ForDouble.Related(LessThan, 0.0);
            Assert.True(neg.Any(LessThan, double.MinValue));
            Assert.False(neg.Any(GreaterThan, double.MaxValue));

            var mi = ForDouble.Related(Equal, double.NegativeInfinity);
            Assert.True(mi.All(LessThan, 0.0));
        }

        [Fact]
        public void TestString_01()
        {
            var notaset = ForString.Related(Equal, "a").Complement();
            var bset = ForString.Related(Equal, "b");
            var intersect = bset.Intersect(notaset);
            Assert.False(intersect.Any(Equal, "c"));
        }

        [Fact]
        public void TestBool_Cov_01()
        {
            var t = ForBool.Related(Equal, true);
            var f = ForBool.Related(Equal, false);
            var em = t.Intersect(f);
            Assert.True(em.IsEmpty);
            var q = t.Intersect(t);
            Assert.Same(t, q);
            IValueSet b = t;
            Assert.Same(b.Intersect(b), b);
            Assert.Same(b.Union(b), b);
            IValueSetFactory bf = ForBool;
        }

        [Fact]
        public void TestByte_Cov_01()
        {
            var s = ForByte.Related(GreaterThan, 10).Intersect(ForByte.Related(LessThan, 100));
            Assert.Equal("[11..99]", s.ToString());
        }

        [Fact]
        public void TestString_Cov_01()
        {
            var s1 = ForString.Related(Equal, "a");
            var s2 = ForString.Related(Equal, "b");
            Assert.True(s1.Intersect(s2).IsEmpty);
            Assert.True(s1.Complement().Union(s2.Complement()).Complement().IsEmpty);
            Assert.Equal(s1.Union(s2).Complement(), s1.Complement().Intersect(s2.Complement()));
            IValueSet b = s1;
            Assert.Same(b.Intersect(b), b);
            Assert.Same(b.Union(b), b);
            IValueSetFactory bf = ForString;
            Assert.False(s1.Union(s2).All(Equal, "a"));
        }

        [Fact]
        public void TestDouble_Cov_01()
        {
            var s1 = ForDouble.Related(LessThan, 3.14d);
            IValueSet b = s1;
            Assert.Same(s1, s1.Intersect(s1));
            Assert.Same(s1, s1.Union(s1));
            var s2 = ForDouble.Related(GreaterThan, 31.4d);
            var s3 = b.Complement().Intersect(s2.Complement());
            Assert.Equal("NaN,[3.1400000000000001..31.399999999999999]", s3.ToString());
            var s4 = b.Union(s2).Complement();
            Assert.Equal(s3, s4);
        }

        [Fact]
        public void TestLong_Cov_01()
        {
            var s1 = ForLong.Related(LessThan, 2);
            Assert.Equal($"[{long.MinValue}..1]", s1.ToString());
            Assert.True(s1.All(LessThan, 2));
            Assert.True(s1.All(LessThanOrEqual, 1));
            Assert.False(s1.All(GreaterThan, 0));
            Assert.False(s1.All(GreaterThanOrEqual, 0));
            Assert.False(s1.All(Equal, 0));

            Assert.False(s1.All(LessThan, -10));
            Assert.False(s1.All(LessThanOrEqual, -10));
            Assert.False(s1.All(GreaterThan, -10));
            Assert.False(s1.All(GreaterThanOrEqual, -10));
            Assert.False(s1.All(Equal, -10));
            Assert.True(s1.All(LessThan, 10));
            Assert.True(s1.All(LessThanOrEqual, 10));
            Assert.False(s1.All(GreaterThan, 10));
            Assert.False(s1.All(GreaterThanOrEqual, 10));
            Assert.False(s1.All(Equal, 10));

            var s2 = ForLong.Related(GreaterThan, -5).Intersect(s1);
            Assert.Equal($"[-4..1]", s2.ToString());
            Assert.False(s2.All(LessThan, -10));
            Assert.False(s2.All(LessThanOrEqual, -10));
            Assert.True(s2.All(GreaterThan, -10));
            Assert.True(s2.All(GreaterThanOrEqual, -10));
            Assert.False(s2.All(Equal, -10));
            Assert.True(s2.All(LessThan, 10));
            Assert.True(s2.All(LessThanOrEqual, 10));
            Assert.False(s2.All(GreaterThan, 10));
            Assert.False(s2.All(GreaterThanOrEqual, 10));
            Assert.False(s2.All(Equal, 10));
        }

        [Fact]
        public void TestNext_Cov_01()
        {
            Assert.Equal("[10..100]", ForSByte.Related(GreaterThanOrEqual, 10).Intersect(ForSByte.Related(LessThanOrEqual, 100)).ToString());
            Assert.Equal("[10..100]", ForShort.Related(GreaterThanOrEqual, 10).Intersect(ForShort.Related(LessThanOrEqual, 100)).ToString());
            Assert.Equal("[10..100]", ForUInt.Related(GreaterThanOrEqual, 10).Intersect(ForUInt.Related(LessThanOrEqual, 100)).ToString());
            Assert.Equal("[10..100]", ForULong.Related(GreaterThanOrEqual, 10).Intersect(ForULong.Related(LessThanOrEqual, 100)).ToString());
            Assert.Equal("[10..100]", ForUShort.Related(GreaterThanOrEqual, 10).Intersect(ForUShort.Related(LessThanOrEqual, 100)).ToString());
            Assert.Equal("[10..100]", ForFloat.Related(GreaterThanOrEqual, 10).Intersect(ForFloat.Related(LessThanOrEqual, 100)).ToString());
            Assert.Equal("[-100..-10]", ForFloat.Related(GreaterThanOrEqual, -100).Intersect(ForFloat.Related(LessThanOrEqual, -10)).ToString());
            Assert.Equal("[-10..10]", ForFloat.Related(GreaterThanOrEqual, -10).Intersect(ForFloat.Related(LessThanOrEqual, 10)).ToString());
        }

        [Fact]
        public void TestAPI_01()
        {
            Assert.Same(ForByte, ForSpecialType(SpecialType.System_Byte));
            Assert.Same(ForSByte, ForSpecialType(SpecialType.System_SByte));
            Assert.Same(ForShort, ForSpecialType(SpecialType.System_Int16));
            Assert.Same(ForUShort, ForSpecialType(SpecialType.System_UInt16));
            Assert.Same(ForInt, ForSpecialType(SpecialType.System_Int32));
            Assert.Same(ForUInt, ForSpecialType(SpecialType.System_UInt32));
            Assert.Same(ForLong, ForSpecialType(SpecialType.System_Int64));
            Assert.Same(ForULong, ForSpecialType(SpecialType.System_UInt64));
            Assert.Same(ForFloat, ForSpecialType(SpecialType.System_Single));
            Assert.Same(ForDouble, ForSpecialType(SpecialType.System_Double));
            Assert.Same(ForString, ForSpecialType(SpecialType.System_String));
            Assert.Same(ForDecimal, ForSpecialType(SpecialType.System_Decimal));
            Assert.Same(ForChar, ForSpecialType(SpecialType.System_Char));
            Assert.Same(ForBool, ForSpecialType(SpecialType.System_Boolean));
            Assert.Null(ForSpecialType(SpecialType.System_Enum));
        }

        [Fact]
        public void TestDecimalRelationsErrorTolerance()
        {
            Assert.Equal("~{}", ForDecimal.Related(LessThan, 0.0m).ToString());
            Assert.Equal("~{}", ForDecimal.Related(LessThanOrEqual, 0.0m).ToString());
            Assert.Equal("~{}", ForDecimal.Related(GreaterThan, 0.0m).ToString());
            Assert.Equal("~{}", ForDecimal.Related(GreaterThanOrEqual, 0.0m).ToString());
        }

        [Fact]
        public void TestFuzz_01()
        {
            const int K = 10;

            for (int i = 0; i < 100; i++)
            {
                var s1 = ForDouble.Random(K, Random);
                var s2 = ForDouble.Random(K, Random);
                var u1 = s1.Union(s2);
                var u2 = s1.Complement().Intersect(s2.Complement()).Complement();
                Assert.Equal(u1, u2);
                var i1 = s1.Intersect(s2);
                var i2 = s1.Complement().Union(s2.Complement()).Complement();
                Assert.Equal(i1, i2);
            }
        }

        [Fact]
        public void TestFuzz_02()
        {
            const int K = 13; // half of the alphabet

            for (int i = 0; i < 100; i++)
            {
                var s1 = randomStringSet(K - 1);
                var s2 = randomStringSet(K + 1);

                var u1 = s1.Union(s2);
                var u2 = s1.Complement().Intersect(s2.Complement()).Complement();
                var u3 = s2.Union(s1);
                var u4 = s2.Complement().Intersect(s1.Complement()).Complement();
                Assert.Equal(u1, u2);
                Assert.Equal(u1, u3);
                Assert.Equal(u1, u4);

                var i1 = s1.Intersect(s2);
                var i2 = s1.Complement().Union(s2.Complement()).Complement();
                var i3 = s2.Intersect(s1);
                var i4 = s2.Complement().Union(s1.Complement()).Complement();
                Assert.Equal(i1, i2);
                Assert.Equal(i1, i3);
                Assert.Equal(i1, i4);

                s1 = s1.Complement();

                u1 = s1.Union(s2);
                u2 = s1.Complement().Intersect(s2.Complement()).Complement();
                u3 = s2.Union(s1);
                u4 = s2.Complement().Intersect(s1.Complement()).Complement();
                Assert.Equal(u1, u2);
                Assert.Equal(u1, u3);
                Assert.Equal(u1, u4);

                i1 = s1.Intersect(s2);
                i2 = s1.Complement().Union(s2.Complement()).Complement();
                i3 = s2.Intersect(s1);
                i4 = s2.Complement().Union(s1.Complement()).Complement();
                Assert.Equal(i1, i2);
                Assert.Equal(i1, i3);
                Assert.Equal(i1, i4);

                s2 = s2.Complement();

                u1 = s1.Union(s2);
                u2 = s1.Complement().Intersect(s2.Complement()).Complement();
                u3 = s2.Union(s1);
                u4 = s2.Complement().Intersect(s1.Complement()).Complement();
                Assert.Equal(u1, u2);
                Assert.Equal(u1, u3);
                Assert.Equal(u1, u4);

                i1 = s1.Intersect(s2);
                i2 = s1.Complement().Union(s2.Complement()).Complement();
                i3 = s2.Intersect(s1);
                i4 = s2.Complement().Union(s1.Complement()).Complement();
                Assert.Equal(i1, i2);
                Assert.Equal(i1, i3);
                Assert.Equal(i1, i4);
            }

            // produce a uniformly random subset of letters of the alphabet of the given size.
            static IValueSet<string> randomStringSet(int size)
            {
                Assert.True(size > 0);
                Assert.True(size < 26);
                IValueSet<string> result = null;
                int need = size;
                for (char c = 'a'; c <= 'z'; c++)
                {
                    int cand = 'z' - c + 1;
                    if (Random.NextDouble() < (1.0 * need / cand))
                    {
                        var added = ForString.Related(Equal, c.ToString());
                        result = result?.Union(added) ?? added;
                        need--;
                    }
                }

                // check that we have `size` members
                int found = 0;
                for (char c = 'a'; c <= 'z'; c++)
                {
                    if (result.Any(Equal, c.ToString()))
                        found++;
                }
                Assert.Equal(size, found);

                Debug.Assert(need == 0);
                return result;
            }
        }

        [Fact]
        public void TestFuzz_03()
        {
            const int K = 13; // half of the alphabet

            for (int i = 0; i < 100; i++)
            {
                var s1 = randomCharSet();
                var s2 = randomCharSet();
                var u1 = s1.Union(s2);
                var u2 = s1.Complement().Intersect(s2.Complement()).Complement();
                Assert.Equal(u1, u2);
                var i1 = s1.Intersect(s2);
                var i2 = s1.Complement().Union(s2.Complement()).Complement();
                Assert.Equal(i1, i2);
            }

            // produce a uniformly random subset of 13 letters of the alphabet.
            IValueSet<char> randomCharSet()
            {
                IValueSet<char> result = null;
                int need = K;
                for (char c = 'a'; c <= 'z'; c++)
                {
                    int cand = 'z' - c + 1;
                    if (Random.NextDouble() < (1.0 * need / cand))
                    {
                        var added = ForChar.Related(Equal, c);
                        result = result?.Union(added) ?? added;
                        need--;
                    }
                }

                // check that we have 13 members
                int found = 0;
                for (char c = 'a'; c <= 'z'; c++)
                {
                    if (result.Any(Equal, c))
                        found++;
                }
                Assert.Equal(K, found);

                Debug.Assert(need == 0);
                return result;
            }
        }
    }
}
