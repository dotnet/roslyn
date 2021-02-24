// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class BoxesTest
    {
        [Fact]
        public void AllBoxesTest()
        {
            // Boolean
            Assert.Same(Boxes.Box(true), Boxes.Box(true));
            Assert.Same(Boxes.Box(false), Boxes.Box(false));
            Assert.NotSame(Boxes.Box(true), Boxes.Box(false));

            // Byte
            Assert.Same(Boxes.Box((byte)0), Boxes.Box((byte)0));
            Assert.NotSame(Boxes.Box((byte)3), Boxes.Box((byte)3));

            // SByte
            Assert.Same(Boxes.Box((sbyte)0), Boxes.Box((sbyte)0));
            Assert.NotSame(Boxes.Box((sbyte)3), Boxes.Box((sbyte)3));

            // Int16
            Assert.Same(Boxes.Box((short)0), Boxes.Box((short)0));
            Assert.NotSame(Boxes.Box((short)3), Boxes.Box((short)3));

            // UInt16
            Assert.Same(Boxes.Box((ushort)0), Boxes.Box((ushort)0));
            Assert.NotSame(Boxes.Box((ushort)3), Boxes.Box((ushort)3));

            // Int32
            Assert.Same(Boxes.Box(0), Boxes.Box(0));
            Assert.Same(Boxes.Box(1), Boxes.Box(1));
            Assert.Same(Boxes.BoxedInt32Zero, Boxes.Box(0));
            Assert.Same(Boxes.BoxedInt32One, Boxes.Box(1));
            Assert.NotSame(Boxes.Box(3), Boxes.Box(3));

            // UInt32
            Assert.Same(Boxes.Box(0u), Boxes.Box(0u));
            Assert.NotSame(Boxes.Box(3u), Boxes.Box(3u));

            // Int64
            Assert.Same(Boxes.Box(0L), Boxes.Box(0L));
            Assert.NotSame(Boxes.Box(3L), Boxes.Box(3L));

            // UInt64
            Assert.Same(Boxes.Box(0UL), Boxes.Box(0UL));
            Assert.NotSame(Boxes.Box(3UL), Boxes.Box(3UL));

            // Single
            Assert.Same(Boxes.Box(0.0f), Boxes.Box(0.0f));
            Assert.NotSame(Boxes.Box(0.0f), Boxes.Box(-0.0f));
            Assert.NotSame(Boxes.Box(1.0f), Boxes.Box(1.0f));

            // Double
            Assert.Same(Boxes.Box(0.0), Boxes.Box(0.0));
            Assert.NotSame(Boxes.Box(0.0), Boxes.Box(-0.0));
            Assert.NotSame(Boxes.Box(1.0), Boxes.Box(1.0));

            // Decimal
            Assert.Same(Boxes.Box(decimal.Zero), Boxes.Box(0m));
            Assert.NotSame(Boxes.Box(0m), Boxes.Box(decimal.Negate(0m)));
            decimal strangeDecimalZero = new decimal(0, 0, 0, false, 10);
            Assert.Equal(decimal.Zero, strangeDecimalZero);
            Assert.NotSame(Boxes.Box(decimal.Zero), Boxes.Box(strangeDecimalZero));

            // Char
            Assert.Same(Boxes.Box('\0'), Boxes.Box('\0'));
            Assert.Same(Boxes.Box('*'), Boxes.Box('*'));
            Assert.Same(Boxes.Box('0'), Boxes.Box('0'));
            Assert.NotSame(Boxes.Box('\u1234'), Boxes.Box('\u1234')); // non ASCII
        }
    }
}
