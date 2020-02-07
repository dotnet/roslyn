// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Assert.Equal($"[{d1}..{d2}]", values.ToString());
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
            Assert.Equal("[A..Z],[a..z]", letters.ToString());
        }

        [Fact]
        public void TestDouble_02()
        {
            Assert.Equal("-Inf", ForDouble.Related(LessThan, double.MinValue).ToString());
            var lt = ForDouble.Related(LessThan, 0.0);
            Assert.Equal("-Inf,[-1.79769313486232E+308..-4.94065645841247E-324]", lt.ToString());
            var gt = ForDouble.Related(GreaterThan, 0.0);
            Assert.Equal("Inf,[4.94065645841247E-324..1.79769313486232E+308]", gt.ToString());
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
            Assert.Equal("-Inf,[-3.402823E+38..-1.401298E-45]", lt.ToString());
            var gt = ForFloat.Related(GreaterThan, 0.0f);
            Assert.Equal("Inf,[1.401298E-45..3.402823E+38]", gt.ToString());
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
            Assert.Same(t.Factory, ForBool);
            IValueSet b = t;
            Assert.Same(b.Factory, ForBool);
            Assert.Same(b.Intersect(b), b);
            Assert.Same(b.Union(b), b);
            IValueSetFactory bf = ForBool;
            Assert.Same(ForBool.All, bf.All);
            Assert.Same(ForBool.None, bf.None);
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
            Assert.Equal(ForString.None, s1.Intersect(s2));
            Assert.Equal(ForString.All, s1.Complement().Union(s2.Complement()));
            Assert.Equal(s1.Union(s2).Complement(), s1.Complement().Intersect(s2.Complement()));
            IValueSet b = s1;
            Assert.Same(s1.Factory, ForString);
            Assert.Same(b.Factory, ForString);
            Assert.Same(b.Intersect(b), b);
            Assert.Same(b.Union(b), b);
            IValueSetFactory bf = ForString;
            Assert.Same(ForString.All, bf.All);
            Assert.Same(ForString.None, bf.None);
            Assert.True(ForString.None.All(Equal, "a"));
            Assert.False(s1.Union(s2).All(Equal, "a"));
        }

        [Fact]
        public void TestFloat_Cov_01()
        {
            var s1 = ForFloat.Related(LessThan, 3.14f);
            IValueSet b = s1;
            Assert.Same(s1.Factory, b.Factory);
            Assert.Same(s1.Factory, ForFloat);
        }

        [Fact]
        public void TestDouble_Cov_01()
        {
            var s1 = ForDouble.Related(LessThan, 3.14d);
            IValueSet b = s1;
            Assert.Same(s1.Factory, b.Factory);
            Assert.Same(s1.Factory, ForDouble);
            Assert.Same(s1, s1.Intersect(s1));
            Assert.Same(s1, s1.Union(s1));
            var s2 = ForDouble.Related(GreaterThan, 31.4d);
            var s3 = b.Complement().Intersect(s2.Complement());
            Assert.Equal("NaN,[3.14..31.4]", s3.ToString());
            var s4 = b.Union(s2).Complement();
            Assert.Equal(s3, s4);
            Assert.Same(b.Factory.All, ForDouble.All);
            Assert.Same(b.Factory.None, ForDouble.None);
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

            IValueSet b = s1;
            Assert.Same(s1.Factory, b.Factory);
            Assert.Same(s1.Factory.None, b.Factory.None);
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
            //Assert.Same(ForByte, ForSpecialType(SpecialType.System_Byte));
            //Assert.Same(ForSByte, ForSpecialType(SpecialType.System_SByte));
            //Assert.Same(ForShort, ForSpecialType(SpecialType.System_Int16));
            //Assert.Same(ForUShort, ForSpecialType(SpecialType.System_UInt16));
            //Assert.Same(ForInt, ForSpecialType(SpecialType.System_Int32));
            //Assert.Same(ForUInt, ForSpecialType(SpecialType.System_UInt32));
            //Assert.Same(ForLong, ForSpecialType(SpecialType.System_Int64));
            //Assert.Same(ForULong, ForSpecialType(SpecialType.System_UInt64));
            //Assert.Same(ForFloat, ForSpecialType(SpecialType.System_Single));
            //Assert.Same(ForDouble, ForSpecialType(SpecialType.System_Double));
            //Assert.Same(ForString, ForSpecialType(SpecialType.System_String));
            //Assert.Same(ForDecimal, ForSpecialType(SpecialType.System_Decimal));
            //Assert.Same(ForChar, ForSpecialType(SpecialType.System_Char));
            //Assert.Same(ForBool, ForSpecialType(SpecialType.System_Boolean));
            //Assert.Null(ForSpecialType(SpecialType.System_Enum));
        }

        [Fact]
        public void TestDecimalRelationsErrorTolerance()
        {
            Assert.Equal("~{}", ForDecimal.Related(LessThan, 0.0m).ToString());
            Assert.Equal("~{}", ForDecimal.Related(LessThanOrEqual, 0.0m).ToString());
            Assert.Equal("~{}", ForDecimal.Related(GreaterThan, 0.0m).ToString());
            Assert.Equal("~{}", ForDecimal.Related(GreaterThanOrEqual, 0.0m).ToString());
        }

        // TODO: test that relationals not supported for decimal
    }
}
