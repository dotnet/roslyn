// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
            // sorted case labels
            private readonly ImmutableArray<KeyValuePair<ConstantValue, object>> _allLabels;

            // range of sorted case labels within this bucket
            private readonly int _startLabelIndex;
            private readonly int _endLabelIndex;

            private readonly bool _isKnownDegenerate;

            /// <summary>
            ///  Degenerate buckets here are buckets with contiguous range of constants
            ///  leading to the same label. Like:
            ///
            ///      case 0:
            ///      case 1:
            ///      case 2:
            ///      case 3:
            ///           DoOneThing();
            ///           break;               
            ///
            ///      case 4:
            ///      case 5:
            ///      case 6:
            ///      case 7:
            ///           DoAnotherThing();
            ///           break;   
            ///  
            ///  NOTE: A trivial bucket with only one case constant is by definition degenerate.
            /// </summary>
            internal bool IsDegenerate
            {
                get
                {
                    return _isKnownDegenerate;
                }
            }

            internal SwitchBucket(ImmutableArray<KeyValuePair<ConstantValue, object>> allLabels, int index)
            {
                _startLabelIndex = index;
                _endLabelIndex = index;
                _allLabels = allLabels;
                _isKnownDegenerate = true;
            }

            private SwitchBucket(ImmutableArray<KeyValuePair<ConstantValue, object>> allLabels, int startIndex, int endIndex)
            {
                Debug.Assert((uint)startIndex < (uint)endIndex);

                _startLabelIndex = startIndex;
                _endLabelIndex = endIndex;
                _allLabels = allLabels;
                _isKnownDegenerate = false;
            }

            internal SwitchBucket(ImmutableArray<KeyValuePair<ConstantValue, object>> allLabels, int startIndex, int endIndex, bool isDegenerate)
            {
                Debug.Assert((uint)startIndex <= (uint)endIndex);
                Debug.Assert((uint)startIndex != (uint)endIndex || isDegenerate);

                _startLabelIndex = startIndex;
                _endLabelIndex = endIndex;
                _allLabels = allLabels;
                _isKnownDegenerate = isDegenerate;
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

            // if a bucket could be split into two degenerate ones
            // specifies a label index where the second bucket would start
            // -1 indicates that the bucket cannot be split into degenerate ones
            //  0 indicates that the bucket is already degenerate
            // 
            // Code Review question: why are we supporting splitting only in two buckets. Why not in more?
            // Explanation:
            //  The input here is a "dense" bucket - the one that previous heuristics 
            //  determined as not worth splitting. 
            //
            //  A dense bucket has rough execution cost of 1 conditional branch (range check) 
            //  and 1 computed branch (which cost roughly the same as conditional one or perhaps more).
            //  The only way to surely beat that cost via splitting is if the bucket can be 
            //  split into 2 degenerate buckets. Then we have just 2 conditional branches.
            //
            //  3 degenerate buckets would require up to 3 conditional branches. 
            //  On some hardware computed jumps may cost significantly more than 
            //  conditional ones (because they are harder to predict or whatever), 
            //  so it could still be profitable, but I did not want to guess that.
            //
            //  Basically if we have 3 degenerate buckets that can be merged into a dense bucket, 
            //  we prefer a dense bucket, which we emit as "switch" opcode.
            //
            internal int DegenerateBucketSplit
            {
                get
                {
                    if (IsDegenerate)
                    {
                        return 0;
                    }

                    Debug.Assert(_startLabelIndex != _endLabelIndex, "1-sized buckets should be already known as degenerate.");

                    var allLabels = this._allLabels;
                    var split = 0;
                    var lastConst = this.StartConstant;
                    var lastLabel = allLabels[_startLabelIndex].Value;

                    for (int idx = _startLabelIndex + 1; idx <= _endLabelIndex; idx++)
                    {
                        var switchLabel = allLabels[idx];

                        if (lastLabel != switchLabel.Value ||
                            !IsContiguous(lastConst, switchLabel.Key))
                        {
                            if (split != 0)
                            {
                                // found another discontinuity, so cannot be split
                                return -1;
                            }

                            split = idx;
                            lastLabel = switchLabel.Value;
                        }

                        lastConst = switchLabel.Key;
                    }

                    return split;
                }
            }

            private bool IsContiguous(ConstantValue lastConst, ConstantValue nextConst)
            {
                if (!lastConst.IsNumeric || !nextConst.IsNumeric)
                {
                    return false;
                }

                return GetBucketSize(lastConst, nextConst) == 2;
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
