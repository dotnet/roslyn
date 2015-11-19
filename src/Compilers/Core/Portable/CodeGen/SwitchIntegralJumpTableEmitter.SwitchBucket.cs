// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal partial struct SwitchIntegralJumpTableEmitter
    {
        private struct SwitchBucket
        {
            // range of sorted case labels within this bucket
            private readonly int _startLabelIndex;
            private readonly int _endLabelIndex;

            // sorted case labels
            private readonly ImmutableArray<KeyValuePair<ConstantValue, object>> _allLabels;

            internal SwitchBucket(ImmutableArray<KeyValuePair<ConstantValue, object>> allLabels, int index)
            {
                _startLabelIndex = index;
                _endLabelIndex = index;
                _allLabels = allLabels;
            }

            private SwitchBucket(ImmutableArray<KeyValuePair<ConstantValue, object>> allLabels, int startIndex, int endIndex)
            {
                Debug.Assert((uint)startIndex < (uint)endIndex);

                _startLabelIndex = startIndex;
                _endLabelIndex = endIndex;
                _allLabels = allLabels;
            }

            internal uint LabelsCount
            {
                get
                {
                    return (uint)(_endLabelIndex - _startLabelIndex + 1);
                }
            }

            internal KeyValuePair<ConstantValue, object> this[int i]
            {
                get
                {
                    Debug.Assert(i < LabelsCount, "index out of range");
                    return _allLabels[i + _startLabelIndex];
                }
            }

            internal ulong BucketSize
            {
                get
                {
                    return GetBucketSize(this.StartConstant, this.EndConstant);
                }
            }

            // Relative cost of the bucket
            // roughly proportional to the number of compares it needs in the success case.
            internal int BucketCost
            {
                get
                {
                    if (_startLabelIndex == _endLabelIndex)
                    {
                        // single element bucket needs exactly one compare
                        return 1;
                    }

                    // dense switch will perform two branches (range check and the actual computed jump)
                    // computed jump is more expensive than a regular conditional branch
                    // based on benchmarks the combined cost seems to be closer to 3
                    //
                    // this also allows in the "mostly sparse" scenario to avoid numerous 
                    // little switches with only 2 labels in them.
                    return 3;
                }
            }

            private static ulong GetBucketSize(ConstantValue startConstant, ConstantValue endConstant)
            {
                Debug.Assert(!BucketOverflowUInt64Limit(startConstant, endConstant));
                Debug.Assert(endConstant.Discriminator == startConstant.Discriminator);

                ulong bucketSize;

                if (startConstant.IsNegativeNumeric || endConstant.IsNegativeNumeric)
                {
                    Debug.Assert(endConstant.Int64Value >= startConstant.Int64Value);
                    bucketSize = unchecked((ulong)(endConstant.Int64Value - startConstant.Int64Value + 1));
                }
                else
                {
                    Debug.Assert(endConstant.UInt64Value >= startConstant.UInt64Value);
                    bucketSize = endConstant.UInt64Value - startConstant.UInt64Value + 1;
                }

                return bucketSize;
            }

            // Check if bucket size exceeds UInt64.MaxValue
            private static bool BucketOverflowUInt64Limit(ConstantValue startConstant, ConstantValue endConstant)
            {
                Debug.Assert(IsValidSwitchBucketConstantPair(startConstant, endConstant));

                if (startConstant.Discriminator == ConstantValueTypeDiscriminator.Int64)
                {
                    return startConstant.Int64Value == Int64.MinValue
                        && endConstant.Int64Value == Int64.MaxValue;
                }
                else if (startConstant.Discriminator == ConstantValueTypeDiscriminator.UInt64)
                {
                    return startConstant.UInt64Value == UInt64.MinValue
                        && endConstant.UInt64Value == UInt64.MaxValue;
                }

                return false;
            }

            // Virtual switch instruction has a max limit of Int32.MaxValue labels
            // Check if bucket size exceeds Int32.MaxValue
            private static bool BucketOverflow(ConstantValue startConstant, ConstantValue endConstant)
            {
                return BucketOverflowUInt64Limit(startConstant, endConstant)
                    || GetBucketSize(startConstant, endConstant) > Int32.MaxValue;
            }

            internal int StartLabelIndex
            {
                get
                {
                    return _startLabelIndex;
                }
            }

            internal int EndLabelIndex
            {
                get
                {
                    return _endLabelIndex;
                }
            }

            internal ConstantValue StartConstant
            {
                get
                {
                    return _allLabels[_startLabelIndex].Key;
                }
            }

            internal ConstantValue EndConstant
            {
                get
                {
                    return _allLabels[_endLabelIndex].Key;
                }
            }

            private static bool IsValidSwitchBucketConstant(ConstantValue constant)
            {
                return constant != null
                    && SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(constant)
                    && !constant.IsNull
                    && !constant.IsString;
            }

            private static bool IsValidSwitchBucketConstantPair(ConstantValue startConstant, ConstantValue endConstant)
            {
                return IsValidSwitchBucketConstant(startConstant)
                    && IsValidSwitchBucketConstant(endConstant)
                    && startConstant.IsUnsigned == endConstant.IsUnsigned;
            }

            private static bool IsSparse(uint labelsCount, ulong bucketSize)
            {
                // TODO: consider changing threshold bucket density to 33%
                return bucketSize >= labelsCount * 2;
            }

            internal static bool MergeIsAdvantageous(SwitchBucket bucket1, SwitchBucket bucket2)
            {
                var startConstant = bucket1.StartConstant;
                var endConstant = bucket2.EndConstant;

                if (BucketOverflow(startConstant, endConstant))
                {
                    // merged bucket would overflow
                    return false;
                }

                uint labelsCount = (uint)(bucket1.LabelsCount + bucket2.LabelsCount);
                ulong bucketSize = GetBucketSize(startConstant, endConstant);

                return !IsSparse(labelsCount, bucketSize);
            }

            /// <summary>
            /// Try to merge with the nextBucket.
            /// If merge results in a better bucket than two original ones, merge and return true.
            /// Else don't merge and return false.
            /// </summary>
            internal bool TryMergeWith(SwitchBucket prevBucket)
            {
                Debug.Assert(prevBucket._endLabelIndex + 1 == _startLabelIndex);
                if (MergeIsAdvantageous(prevBucket, this))
                {
                    this = new SwitchBucket(_allLabels, prevBucket._startLabelIndex, _endLabelIndex);
                    return true;
                }

                return false;
            }
        }
    }
}
