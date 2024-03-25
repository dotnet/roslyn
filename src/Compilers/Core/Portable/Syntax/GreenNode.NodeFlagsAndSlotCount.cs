// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class GreenNode
    {
        /// <summary>
        /// Combination of <see cref="NodeFlags"/> and <see cref="SlotCount"/> stored in a single 16bit value.
        /// </summary>
        private struct NodeFlagsAndSlotCount
        {
            /// <summary>
            /// 4 bits for the SlotCount.  This allows slot counts of 0-14 to be stored as a direct byte.  All 1s
            /// indicates that the slot count must be computed.
            /// </summary>
            private const ushort SlotCountMask = 0b1111000000000000;
            private const ushort NodeFlagsMask = 0b0000111111111111;

            private const int SlotCountShift = 12;

            /// <summary>
            /// 12 bits for the NodeFlags.  This allows for up to 12 distinct bits to be stored to designate interesting
            /// aspects of a node.
            /// </summary>

            /// <summary>
            /// CCCCFFFFFFFFFFFF for Count bits then Flag bits.
            /// </summary>
            private ushort _data;

            /// <summary>
            /// Returns the slot count if it was small enough to be stored directly in this object.  Otherwise, returns
            /// <see cref="SlotCountTooLarge"/> to indicate it could not be directly stored.
            /// </summary>
            public byte SmallSlotCount
            {
                readonly get
                {
                    var shifted = _data >> SlotCountShift;
                    Debug.Assert(shifted <= SlotCountTooLarge);
                    return (byte)shifted;
                }

                set
                {
                    if (value > SlotCountTooLarge)
                        value = SlotCountTooLarge;

                    // Clear out everything but the node-flags, and then assign into the slot-count segment.
                    _data = (ushort)((_data & NodeFlagsMask) | (value << SlotCountShift));
                }
            }

            public NodeFlags NodeFlags
            {
                readonly get
                {
                    return (NodeFlags)(_data & NodeFlagsMask);
                }

                set
                {
                    Debug.Assert((ushort)value <= NodeFlagsMask);

                    // Clear out everything but the slot-count, and then assign into the node-flags segment.
                    _data = (ushort)((_data & SlotCountMask) | (ushort)value);
                }
            }
        }
    }
}
