// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public void TestNE_01(int i1)
        {
            IValueSet<int> values = ForInt.Related(NotEqual, i1);
            var expected =
                (i1 == int.MinValue) ? $"[{i1 + 1}..{int.MaxValue}]" :
                (i1 == int.MaxValue) ? $"[{int.MinValue}..{i1 - 1}]" :
                $"[{int.MinValue}..{i1 - 1}],[{i1 + 1}..{int.MaxValue}]";
            Assert.Equal(expected, values.ToString());
        }

        [Fact]
        public void TestNE_02()
        {
            for (int i = 0; i < 100; i++)
            {
                int i1 = Random.Next(int.MinValue + 1, int.MaxValue);
                IValueSet<int> values = ForInt.Related(NotEqual, i1);
                Assert.Equal($"[{int.MinValue}..{i1 - 1}],[{i1 + 1}..{int.MaxValue}]", values.ToString());
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
                    Assert.Equal(val != i1 || val != i2, values.Any(NotEqual, val));
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
            Assert.Equal("Inf", ForDouble.Related(Equal, double.PositiveInfinity).ToString());
            Assert.Equal("Inf", ForFloat.Related(Equal, float.PositiveInfinity).ToString());
            Assert.Equal("-Inf", ForDouble.Related(Equal, double.NegativeInfinity).ToString());
            Assert.Equal("-Inf", ForFloat.Related(Equal, float.NegativeInfinity).ToString());
        }

        // TODO: test that relationals not supported for decimal
        // TODO: test other integral types
        // TODO: test bool
    }
}
