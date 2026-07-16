// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    using static BinaryOperatorKind;
    using static Microsoft.CodeAnalysis.CSharp.ValueSetFactory;

    /// <summary>
    /// Test some internal implementation data structures used in <see cref="DecisionDagBuilder"/>.
    /// </summary>
    public class ValueSetTests
    {
        private static readonly Random Random = new Random();

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
            IConstantValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1);
            Assert.Equal($"[{i1}..{int.MaxValue}]", values.ToString());
        }

        [Fact]
        public void TestGE_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                IConstantValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1);
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
            IConstantValueSet<int> values = ForInt.Related(GreaterThan, i1);
            Assert.Equal((i1 == int.MaxValue) ? "" : $"[{i1 + 1}..{int.MaxValue}]", values.ToString());
        }

        [Fact]
        public void TestGT_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                IConstantValueSet<int> values = ForInt.Related(GreaterThan, i1);
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
            IConstantValueSet<int> values = ForInt.Related(LessThanOrEqual, i1);
            Assert.Equal($"[{int.MinValue}..{i1}]", values.ToString());
        }

        [Fact]
        public void TestLE_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue) + 1;
                IConstantValueSet<int> values = ForInt.Related(LessThanOrEqual, i1);
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
            IConstantValueSet<int> values = ForInt.Related(LessThan, i1);
            Assert.Equal((i1 == int.MinValue) ? "" : $"[{int.MinValue}..{i1 - 1}]", values.ToString());
        }

        [Fact]
        public void TestLT_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue) + 1;
                IConstantValueSet<int> values = ForInt.Related(LessThan, i1);
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
            IConstantValueSet<int> values = ForInt.Related(Equal, i1);
            Assert.Equal($"[{i1}..{i1}]", values.ToString());
        }

        [Fact]
        public void TestEQ_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue, int.MaxValue);
                IConstantValueSet<int> values = ForInt.Related(Equal, i1);
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
                IConstantValueSet<int> values1 = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal($"[{i1}..{i2}]", values1.ToString());
                IConstantValueSet<int> values2 = ForInt.Related(LessThanOrEqual, i2).Intersect(ForInt.Related(GreaterThanOrEqual, i1));
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
                IConstantValueSet<int> values1 = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal($"", values1.ToString());
                IConstantValueSet<int> values2 = ForInt.Related(LessThanOrEqual, i2).Intersect(ForInt.Related(GreaterThanOrEqual, i1));
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
                IConstantValueSet<int> values1 = ForInt.Related(LessThanOrEqual, i1).Union(ForInt.Related(GreaterThanOrEqual, i2));
                Assert.Equal($"[{int.MinValue}..{i1}],[{i2}..{int.MaxValue}]", values1.ToString());
                IConstantValueSet<int> values2 = ForInt.Related(GreaterThanOrEqual, i2).Union(ForInt.Related(LessThanOrEqual, i1));
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
                IConstantValueSet<int> values1 = ForInt.Related(LessThanOrEqual, i1).Union(ForInt.Related(GreaterThanOrEqual, i2));
                Assert.Equal($"[{int.MinValue}..{int.MaxValue}]", values1.ToString());
                IConstantValueSet<int> values2 = ForInt.Related(GreaterThanOrEqual, i2).Union(ForInt.Related(LessThanOrEqual, i1));
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
                IConstantValueSet<int> values1 = ForInt.Related(LessThanOrEqual, i1).Union(ForInt.Related(GreaterThanOrEqual, i2));
                Assert.Equal($"[{int.MinValue}..{i1}],[{i2}..{int.MaxValue}]", values1.ToString());
                IConstantValueSet<int> values2 = values1.Complement();
                Assert.Equal(values1, values2.Complement());
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
                IConstantValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
                Assert.Equal($"[{i1}..{i2}]", values.ToString());
                test(int.MinValue);
                if (i1 != int.MinValue) test(i1 - 1);
                test(i1);
                test(i1 + 1);
                test(int.MaxValue);
                if (i2 != int.MinValue) test(i2 - 1);
                test(i2);
                test(i2 + 1);
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
                IConstantValueSet<int> values = ForInt.Related(GreaterThanOrEqual, i1).Intersect(ForInt.Related(LessThanOrEqual, i2));
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
                IConstantValueSet<double> values = ForDouble.Related(GreaterThanOrEqual, d1).Intersect(ForDouble.Related(LessThanOrEqual, d2));
                Assert.Equal(FormattableString.Invariant($"[{d1:G17}..{d2:G17}]"), values.ToString());
            }
        }

        [Fact]
        public void TestChar_01()
        {
            IConstantValueSet<char> gea1 = ForChar.Related(GreaterThanOrEqual, 'a');
            IConstantValueSet<char> lez1 = ForChar.Related(LessThanOrEqual, 'z');
            IConstantValueSet<char> gea2 = ForChar.Related(GreaterThanOrEqual, 'A');
            IConstantValueSet<char> lez2 = ForChar.Related(LessThanOrEqual, 'Z');
            var letters = gea1.Intersect(lez1).Union(gea2.Intersect(lez2));
            Assert.Equal("['A'..'Z'],['a'..'z']", letters.ToString());
        }

        [Fact]
        public void TestDouble_02()
        {
            Assert.Equal("[-Inf..-Inf]", ForDouble.Related(LessThan, double.MinValue).ToString());
            var lt = ForDouble.Related(LessThan, 0.0);
            Assert.Equal(FormattableString.Invariant($"[-Inf..{-double.Epsilon:G17}]"), lt.ToString());
            var gt = ForDouble.Related(GreaterThan, 0.0);
            Assert.Equal(FormattableString.Invariant($"[{double.Epsilon:G17}..Inf]"), gt.ToString());
            var eq = ForDouble.Related(Equal, 0.0);
            Assert.Equal("[0..0]", eq.ToString());
            var none = lt.Complement().Intersect(gt.Complement()).Intersect(eq.Complement());
            Assert.Equal("NaN", none.ToString());
            Assert.False(none.IsEmpty);
        }

        [Fact]
        public void TestFloat_01()
        {
            Assert.Equal("[-Inf..-Inf]", ForFloat.Related(LessThan, float.MinValue).ToString());
            var lt = ForFloat.Related(LessThan, 0.0f);
            Assert.Equal(FormattableString.Invariant($"[-Inf..{-float.Epsilon:G9}]"), lt.ToString());
            var gt = ForFloat.Related(GreaterThan, 0.0f);
            Assert.Equal(FormattableString.Invariant($"[{float.Epsilon:G9}..Inf]"), gt.ToString());
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
            Assert.Equal("[Inf..Inf]", ForDouble.Related(Equal, double.PositiveInfinity).ToString());
            Assert.Equal("[Inf..Inf]", ForFloat.Related(Equal, float.PositiveInfinity).ToString());
            Assert.Equal("[-Inf..-Inf]", ForDouble.Related(Equal, double.NegativeInfinity).ToString());
            Assert.Equal("[-Inf..-Inf]", ForFloat.Related(Equal, float.NegativeInfinity).ToString());
        }

        [Fact]
        public void TestDouble_04()
        {
            var neg = ForDouble.Related(LessThan, 0.0);
            Assert.True(neg.Any(LessThan, double.MinValue));
            Assert.False(neg.Any(GreaterThan, double.MaxValue));

            var mi = ForDouble.Related(Equal, double.NegativeInfinity);
            Assert.True(mi.All(LessThan, 0.0));
            Assert.True(mi.Any(LessThan, 0.0));
            Assert.True(mi.All(LessThanOrEqual, 0.0));
            Assert.True(mi.Any(LessThanOrEqual, 0.0));
            Assert.False(mi.All(GreaterThan, 0.0));
            Assert.False(mi.Any(GreaterThan, 0.0));
            Assert.False(mi.All(GreaterThanOrEqual, 0.0));
            Assert.False(mi.Any(GreaterThanOrEqual, 0.0));

            var i = ForDouble.Related(Equal, double.PositiveInfinity);
            Assert.False(i.All(LessThan, 0.0));
            Assert.False(i.Any(LessThan, 0.0));
            Assert.False(i.All(LessThanOrEqual, 0.0));
            Assert.False(i.Any(LessThanOrEqual, 0.0));
            Assert.True(i.All(GreaterThan, 0.0));
            Assert.True(i.Any(GreaterThan, 0.0));
            Assert.True(i.All(GreaterThanOrEqual, 0.0));
            Assert.True(i.Any(GreaterThanOrEqual, 0.0));
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
            Assert.Same(ForNint, ForSpecialType(SpecialType.System_IntPtr, isNative: true));
            Assert.Same(ForNuint, ForSpecialType(SpecialType.System_UIntPtr, isNative: true));
            Assert.Null(ForSpecialType(SpecialType.System_Enum));
        }

        [Fact]
        public void TestDecimalRelations_01()
        {
            Assert.Equal("[-79228162514264337593543950335..-0.0000000000000000000000000001]", ForDecimal.Related(LessThan, 0.0m).ToString());
            Assert.Equal("[-79228162514264337593543950335..0.0000000000000000000000000000]", ForDecimal.Related(LessThanOrEqual, 0.0m).ToString());
            Assert.Equal("[0.0000000000000000000000000001..79228162514264337593543950335]", ForDecimal.Related(GreaterThan, 0.0m).ToString());
            Assert.Equal("[0.0000000000000000000000000000..79228162514264337593543950335]", ForDecimal.Related(GreaterThanOrEqual, 0.0m).ToString());
        }

        [Fact]
        public void TestNintRelations_01()
        {
            Assert.Equal("Small,[-2147483648..9]", ForNint.Related(LessThan, 10).ToString());
            Assert.Equal("Small,[-2147483648..10]", ForNint.Related(LessThanOrEqual, 10).ToString());
            Assert.Equal("[11..2147483647],Large", ForNint.Related(GreaterThan, 10).ToString());
            Assert.Equal("[10..2147483647],Large", ForNint.Related(GreaterThanOrEqual, 10).ToString());
        }

        [Fact]
        public void TestNuintRelations_01()
        {
            Assert.Equal("[0..9]", ForNuint.Related(LessThan, 10).ToString());
            Assert.Equal("[0..10]", ForNuint.Related(LessThanOrEqual, 10).ToString());
            Assert.Equal("[11..4294967295],Large", ForNuint.Related(GreaterThan, 10).ToString());
            Assert.Equal("[10..4294967295],Large", ForNuint.Related(GreaterThanOrEqual, 10).ToString());
        }

        [Fact]
        public void TestDecimalEdges_01()
        {
            for (byte scale = 0; scale < 29; scale++)
            {
                var l = new decimal(~0, ~0, ~0, false, scale);
                check(l);
                l = new decimal(~0, ~0, ~0, true, scale);
                check(l);
            }

            for (byte scale = 0; scale < 29; scale++)
            {
                var l = new decimal(unchecked((int)0x99999999), unchecked((int)0x99999999), 0x19999999, false, scale);
                check(l);
                l = new decimal(unchecked((int)0x99999999), unchecked((int)0x99999999), 0x19999999, true, scale);
                check(l);
                l = new decimal(unchecked((int)0x99999998), unchecked((int)0x99999999), 0x19999999, false, scale);
                check(l);
                l = new decimal(unchecked((int)0x99999998), unchecked((int)0x99999999), 0x19999999, true, scale);
                check(l);
                l = new decimal(unchecked((int)0x9999999A), unchecked((int)0x99999999), 0x19999999, false, scale);
                check(l);
                l = new decimal(unchecked((int)0x9999999A), unchecked((int)0x99999999), 0x19999999, true, scale);
                check(l);
            }

            for (int high = 0; high < 2; high++)
            {
                for (int mid = 0; mid < 2; mid++)
                {
                    for (int p2 = 0; p2 < 32; p2++)
                    {
                        int low = 1 << p2;
                        var l = new decimal(low, mid, high, false, 28);
                        check(l);
                        l = new decimal(low, mid, high, true, 28);
                        check(l);
                    }
                }
            }

            void check(decimal d)
            {
                Assert.False(ForDecimal.Related(LessThan, d).Any(Equal, d));
                Assert.True(ForDecimal.Related(LessThanOrEqual, d).Any(Equal, d));
                Assert.False(ForDecimal.Related(GreaterThan, d).Any(Equal, d));
                Assert.True(ForDecimal.Related(GreaterThanOrEqual, d).Any(Equal, d));
            }
        }

        [Fact]
        public void TestNumbers_Fuzz_01()
        {
            var Random = new Random(123445);

            foreach (var fac in new IConstantValueSetFactory[] {
                ForByte, ForSByte, ForShort, ForUShort,
                ForInt, ForUInt, ForLong, ForULong,
                ForFloat, ForDouble, ForDecimal, ForNint,
                ForNuint, ForChar, ForLength,
                })
            {
                for (int i = 0; i < 100; i++)
                {
                    var s1 = fac.Random(10, Random);
                    var s2 = fac.Random(10, Random);
                    var u1 = s1.Union(s2);
                    var u2 = s1.Complement().Intersect(s2.Complement()).Complement();
                    Assert.Equal(u1, u2);
                    var i1 = s1.Intersect(s2);
                    var i2 = s1.Complement().Union(s2.Complement()).Complement();
                    Assert.Equal(i1, i2);
                }
            }
        }

        [Fact]
        public void TestNumbers_Fuzz_02()
        {
            foreach (var fac in new IConstantValueSetFactory[] {
                ForByte, ForSByte, ForShort, ForUShort,
                ForInt, ForUInt, ForLong, ForULong,
                ForDecimal, ForNint,
                ForNuint, ForChar, ForLength })
            {
                for (int i = 0; i < 100; i++)
                {
                    ConstantValue value = fac.RandomValue(Random);
                    var s1 = fac.Related(LessThan, value);
                    var s2 = fac.Related(GreaterThanOrEqual, value);
                    Assert.Equal(s1.Complement(), s2);
                    Assert.Equal(s2.Complement(), s1);
                    Assert.True(s2.Any(Equal, value));
                    Assert.False(s1.Any(Equal, value));
                    Assert.True(s1.All(LessThan, value));
                    Assert.False(s2.Any(LessThan, value));
                    Assert.True(s2.All(GreaterThanOrEqual, value));
                    Assert.False(s1.Any(GreaterThanOrEqual, value));

                    s1 = fac.Related(GreaterThan, value);
                    s2 = fac.Related(LessThanOrEqual, value);
                    Assert.Equal(s1.Complement(), s2);
                    Assert.Equal(s2.Complement(), s1);
                    Assert.True(s2.Any(Equal, value));
                    Assert.False(s1.Any(Equal, value));
                    Assert.True(s1.All(GreaterThan, value));
                    Assert.False(s2.Any(GreaterThan, value));
                    Assert.True(s2.All(LessThanOrEqual, value));
                    Assert.False(s1.Any(LessThanOrEqual, value));
                }
            }
        }

        [Fact]
        public void TestString_Fuzz_02()
        {
            for (int i = 0; i < 100; i++)
            {
                var s1 = ForString.Random(9, Random);
                var s2 = ForString.Random(11, Random);

                Assert.Equal(s1.Complement().Complement(), s1);

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

                s1 = (IConstantValueSet)s1.Complement();

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

                s2 = (IConstantValueSet)s2.Complement();

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
        }

        [Fact]
        public void TestAnyFuzz_01()
        {
            for (int i = 0; i < 100; i++)
            {
                var s1 = ForInt.Related(BinaryOperatorKind.Equal, i);
                Assert.True(s1.Any(LessThan, i + 1));
                Assert.False(s1.Any(LessThan, i));
                Assert.False(s1.Any(LessThan, i - 1));
                Assert.True(s1.Any(LessThanOrEqual, i + 1));
                Assert.True(s1.Any(LessThanOrEqual, i));
                Assert.False(s1.Any(LessThanOrEqual, i - 1));
                Assert.False(s1.Any(GreaterThan, i + 1));
                Assert.False(s1.Any(GreaterThan, i));
                Assert.True(s1.Any(GreaterThan, i - 1));
                Assert.False(s1.Any(GreaterThanOrEqual, i + 1));
                Assert.True(s1.Any(GreaterThanOrEqual, i));
                Assert.True(s1.Any(GreaterThanOrEqual, i - 1));
            }
        }

        [Fact]
        public void TestAnyFuzz_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int j = Random.Next();
                var s1 = ForInt.Related(BinaryOperatorKind.Equal, j);
                Assert.True(s1.Any(LessThan, j + 1));
                Assert.False(s1.Any(LessThan, j));
                Assert.False(s1.Any(LessThan, j - 1));
                Assert.True(s1.Any(LessThanOrEqual, j + 1));
                Assert.True(s1.Any(LessThanOrEqual, j));
                Assert.False(s1.Any(LessThanOrEqual, j - 1));
                Assert.False(s1.Any(GreaterThan, j + 1));
                Assert.False(s1.Any(GreaterThan, j));
                Assert.True(s1.Any(GreaterThan, j - 1));
                Assert.False(s1.Any(GreaterThanOrEqual, j + 1));
                Assert.True(s1.Any(GreaterThanOrEqual, j));
                Assert.True(s1.Any(GreaterThanOrEqual, j - 1));
            }
        }

        [Fact]
        public void TestAllFuzz_01()
        {
            for (int i = 0; i < 100; i++)
            {
                var s1 = ForInt.Related(BinaryOperatorKind.LessThan, i);
                Assert.True(s1.All(LessThan, i + 1));
                Assert.True(s1.All(LessThan, i));
                Assert.False(s1.All(LessThan, i - 1));
                Assert.True(s1.All(LessThanOrEqual, i + 1));
                Assert.True(s1.All(LessThanOrEqual, i));
                Assert.True(s1.All(LessThanOrEqual, i - 1));
                Assert.False(s1.All(LessThanOrEqual, i - 2));
                s1 = ForInt.Related(BinaryOperatorKind.GreaterThan, i);
                Assert.False(s1.All(GreaterThan, i + 1));
                Assert.True(s1.All(GreaterThan, i));
                Assert.True(s1.All(GreaterThan, i - 1));
                Assert.False(s1.All(GreaterThanOrEqual, i + 2));
                Assert.True(s1.All(GreaterThanOrEqual, i + 1));
                Assert.True(s1.All(GreaterThanOrEqual, i));
                Assert.True(s1.All(GreaterThanOrEqual, i - 1));
            }
        }

        [Fact]
        public void TestAllFuzz_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int j = Random.Next(0, int.MaxValue - 1);
                var s1 = ForInt.Related(BinaryOperatorKind.LessThan, j);
                Assert.True(s1.All(LessThan, j + 1));
                Assert.True(s1.All(LessThan, j));
                Assert.False(s1.All(LessThan, j - 1));
                Assert.True(s1.All(LessThanOrEqual, j + 1));
                Assert.True(s1.All(LessThanOrEqual, j));
                Assert.True(s1.All(LessThanOrEqual, j - 1));
                Assert.False(s1.All(LessThanOrEqual, j - 2));
                s1 = ForInt.Related(BinaryOperatorKind.GreaterThan, j);
                Assert.False(s1.All(GreaterThan, j + 1));
                Assert.True(s1.All(GreaterThan, j));
                Assert.True(s1.All(GreaterThan, j - 1));
                Assert.False(s1.All(GreaterThanOrEqual, j + 2));
                Assert.True(s1.All(GreaterThanOrEqual, j + 1));
                Assert.True(s1.All(GreaterThanOrEqual, j));
                Assert.True(s1.All(GreaterThanOrEqual, j - 1));
            }
        }

        [Fact]
        public void TestAllFuzz_03()
        {
            for (int i = 0; i < 100; i++)
            {
                var s1 = ForInt.Related(BinaryOperatorKind.Equal, i);
                Assert.True(s1.All(LessThan, i + 1));
                Assert.False(s1.All(LessThan, i));
                Assert.False(s1.All(LessThan, i - 1));
                Assert.True(s1.All(LessThanOrEqual, i + 1));
                Assert.True(s1.All(LessThanOrEqual, i));
                Assert.False(s1.All(LessThanOrEqual, i - 1));
                Assert.False(s1.All(GreaterThan, i + 1));
                Assert.False(s1.All(GreaterThan, i));
                Assert.True(s1.All(GreaterThan, i - 1));
                Assert.False(s1.All(GreaterThanOrEqual, i + 1));
                Assert.True(s1.All(GreaterThanOrEqual, i));
                Assert.True(s1.All(GreaterThanOrEqual, i - 1));
            }
        }

        [Fact]
        public void TestAllFuzz_04()
        {
            for (int i = 0; i < 100; i++)
            {
                int j = Random.Next(0, int.MaxValue - 1);
                var s1 = ForInt.Related(BinaryOperatorKind.Equal, j);
                Assert.True(s1.All(LessThan, j + 1));
                Assert.False(s1.All(LessThan, j));
                Assert.False(s1.All(LessThan, j - 1));
                Assert.True(s1.All(LessThanOrEqual, j + 1));
                Assert.True(s1.All(LessThanOrEqual, j));
                Assert.False(s1.All(LessThanOrEqual, j - 1));
                Assert.False(s1.All(GreaterThan, j + 1));
                Assert.False(s1.All(GreaterThan, j));
                Assert.True(s1.All(GreaterThan, j - 1));
                Assert.False(s1.All(GreaterThanOrEqual, j + 1));
                Assert.True(s1.All(GreaterThanOrEqual, j));
                Assert.True(s1.All(GreaterThanOrEqual, j - 1));
            }
        }

        [Fact]
        public void DoNotCrashOnBadInput()
        {
            // For error recovery, do not throw exceptions on bad inputs.
            var ctors = new IConstantValueSetFactory[]
            {
                    ForByte,
                    ForSByte,
                    ForChar,
                    ForShort,
                    ForUShort,
                    ForInt,
                    ForUInt,
                    ForLong,
                    ForULong,
                    ForBool,
                    ForFloat,
                    ForDouble,
                    ForString,
                    ForDecimal,
                    ForNint,
                    ForNuint,
                    ForLength,
            };
            ConstantValue badConstant = ConstantValue.Bad;
            foreach (IConstantValueSetFactory fac in ctors)
            {
                foreach (BinaryOperatorKind relation in new[] { LessThan, Equal, NotEqual })
                {
                    IConstantValueSet set = fac.Related(relation, badConstant);
                    _ = set.All(relation, badConstant);
                    _ = set.Any(relation, badConstant);
                }
            }
        }
    }
}
