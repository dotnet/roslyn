// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal enum EnumOverflowKind { NoOverflow, OverflowReport, OverflowIgnore }

    internal static class EnumConstantHelper
    {
        /// <summary>
        /// Generate a ConstantValue of the same integer type as the argument
        /// and offset by the given non-negative amount. Return ConstantValue.Bad
        /// if the generated constant would be outside the valid range of the type.
        /// </summary>
        internal static EnumOverflowKind OffsetValue(ConstantValue constantValue, uint offset, out ConstantValue offsetValue)
        {
            Debug.Assert(!constantValue.IsBad);
            Debug.Assert(offset > 0);

            offsetValue = ConstantValue.Bad;

            EnumOverflowKind overflowKind;
            switch (constantValue.Discriminator)
            {
                case ConstantValueTypeDiscriminator.SByte:
                    {
                        long previous = constantValue.SByteValue;
                        overflowKind = CheckOverflow(sbyte.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((sbyte)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.Byte:
                    {
                        ulong previous = constantValue.ByteValue;
                        overflowKind = CheckOverflow(byte.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((byte)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.Int16:
                    {
                        long previous = constantValue.Int16Value;
                        overflowKind = CheckOverflow(short.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((short)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.UInt16:
                    {
                        ulong previous = constantValue.UInt16Value;
                        overflowKind = CheckOverflow(ushort.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((ushort)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.Int32:
                    {
                        long previous = constantValue.Int32Value;
                        overflowKind = CheckOverflow(int.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((int)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.UInt32:
                    {
                        ulong previous = constantValue.UInt32Value;
                        overflowKind = CheckOverflow(uint.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((uint)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.Int64:
                    {
                        long previous = constantValue.Int64Value;
                        overflowKind = CheckOverflow(long.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((long)(previous + offset));
                        }
                    }
                    break;
                case ConstantValueTypeDiscriminator.UInt64:
                    {
                        ulong previous = constantValue.UInt64Value;
                        overflowKind = CheckOverflow(ulong.MaxValue, previous, offset);
                        if (overflowKind == EnumOverflowKind.NoOverflow)
                        {
                            offsetValue = ConstantValue.Create((ulong)(previous + offset));
                        }
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(constantValue.Discriminator);
            }

            return overflowKind;
        }

        private static EnumOverflowKind CheckOverflow(long maxOffset, long previous, uint offset)
        {
            Debug.Assert(maxOffset >= previous);
            return CheckOverflow(unchecked((ulong)(maxOffset - previous)), offset);
        }

        private static EnumOverflowKind CheckOverflow(ulong maxOffset, ulong previous, uint offset)
        {
            Debug.Assert(maxOffset >= previous);
            return CheckOverflow(maxOffset - previous, offset);
        }

        private static EnumOverflowKind CheckOverflow(ulong maxOffset, uint offset)
        {
            return (offset <= maxOffset) ?
                EnumOverflowKind.NoOverflow :
                (((offset - 1) == maxOffset) ? EnumOverflowKind.OverflowReport : EnumOverflowKind.OverflowIgnore);
        }
    }
}
