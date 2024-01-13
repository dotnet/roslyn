// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Class for emitting the switch jump table for switch statements with integral governing type
    /// </summary>
    internal partial struct SwitchIntegralJumpTableEmitter
    {
        private readonly ILBuilder _builder;

        /// <summary>
        /// Switch key for the jump table
        /// </summary>
        private readonly LocalOrParameter _key;

        /// <summary>
        /// Primitive type of the switch key
        /// </summary>
        private readonly Cci.PrimitiveTypeCode _keyTypeCode;

        /// <summary>
        /// Fall through label for the jump table
        /// </summary>
        private readonly object _fallThroughLabel;

        /// <summary>
        /// Integral case labels sorted and indexed by their ConstantValue
        /// </summary>
        private readonly ImmutableArray<KeyValuePair<ConstantValue, object>> _sortedCaseLabels;

        // threshold at which binary search stops partitioning.
        // if a search leaf has less than LinearSearchThreshold buckets
        // we just go through buckets linearly.
        // We chose 3 here because it is where number of branches to reach fall-through 
        // is the same for linear and binary search.
        private const int LinearSearchThreshold = 3;

        internal SwitchIntegralJumpTableEmitter(
            ILBuilder builder,
            KeyValuePair<ConstantValue, object>[] caseLabels,
            object fallThroughLabel,
            Cci.PrimitiveTypeCode keyTypeCode,
            LocalOrParameter key)
        {
            _builder = builder;
            _key = key;
            _keyTypeCode = keyTypeCode;
            _fallThroughLabel = fallThroughLabel;

            // Sort the switch case labels, see comments below for more details.
            Debug.Assert(caseLabels.Length > 0);
            Array.Sort(caseLabels, CompareIntegralSwitchLabels);
            _sortedCaseLabels = ImmutableArray.Create(caseLabels);
        }

        internal void EmitJumpTable()
        {
            //  For emitting the switch statement (integral governing type) jump table with a non-constant
            //  switch expression, we can use a naive approach and generate a single big MSIL switch instruction
            //  with all the case labels and fall through label. However, this approach can be optimized
            //  to improve both the code size and speed using the following optimization steps:

            //  a)	Sort the switch case labels based on their constant values.

            //  b)	Divide the sorted switch labels into buckets with good enough density (>50%). For example:
            //      switch(..)
            //      {
            //          case 1:
            //          case 100:
            //              break;
            //          case 2:
            //          case 4:
            //              break;
            //          case 200:
            //          case 201:
            //          case 202:
            //              break;
            //      }

            //      can be divided into 3 buckets: (1, 2, 4) (100) (200, 201, 202).
            //      We do this bucketing so that we have reasonable size jump tables for generated switch instructions.

            //  c)	After bucketing, generate code to perform a binary search on these buckets array, 
            //      emitting conditional jumps if current bucket sub-array has more than one bucket and
            //      emitting the switch instruction when we are down to a single bucket in the sub-array.

            // (a) Sort switch labels: This was done in the constructor

            Debug.Assert(!_sortedCaseLabels.IsEmpty);
            var sortedCaseLabels = _sortedCaseLabels;

            int endLabelIndex = sortedCaseLabels.Length - 1;
            int startLabelIndex;

            // Check for a label with ConstantValue.Null.
            // Sorting ensures that if we do have one, it will be
            // the first label in the sorted list.
            if (sortedCaseLabels[0].Key != ConstantValue.Null)
            {
                startLabelIndex = 0;
            }
            else
            {
                // Skip null label for emitting switch table header.
                // We should have inserted a conditional branch to 'null' label during rewriting.
                // See LocalRewriter.MakeSwitchStatementWithNullableExpression
                startLabelIndex = 1;
            }

            if (startLabelIndex <= endLabelIndex)
            {
                // We have at least one non-null case label, emit jump table

                // (b) Generate switch buckets
                ImmutableArray<SwitchBucket> switchBuckets = this.GenerateSwitchBuckets(startLabelIndex, endLabelIndex);

                // (c) Emit switch buckets
                this.EmitSwitchBuckets(switchBuckets, 0, switchBuckets.Length - 1);
            }
            else
            {
                _builder.EmitBranch(ILOpCode.Br, _fallThroughLabel);
            }
        }

        #region "Sorting switch labels"

        private static int CompareIntegralSwitchLabels(KeyValuePair<ConstantValue, object> first, KeyValuePair<ConstantValue, object> second)
        {
            ConstantValue firstConstant = first.Key;
            ConstantValue secondConstant = second.Key;

            RoslynDebug.Assert(firstConstant != null);
            Debug.Assert(SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(firstConstant)
                && !firstConstant.IsString);

            RoslynDebug.Assert(secondConstant != null);
            Debug.Assert(SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(secondConstant)
                && !secondConstant.IsString);

            return SwitchConstantValueHelper.CompareSwitchCaseLabelConstants(firstConstant, secondConstant);
        }

        #endregion

        #region "Switch bucketing methods"

        // Bucketing algorithm:

        //  Start with empty stack of buckets.

        //  While there are still labels

        //      If bucket from remaining labels is dense
        //             Create a newBucket from remaining labels
        //      Else
        //             Create a singleton newBucket from the next label

        //      While the top bucket on stack can be merged with newBucket into a dense bucket
        //          merge top bucket on stack into newBucket, and pop bucket from stack
        //      End While

        //      Push newBucket on stack

        //  End While

        private ImmutableArray<SwitchBucket> GenerateSwitchBuckets(int startLabelIndex, int endLabelIndex)
        {
            Debug.Assert(startLabelIndex >= 0 && startLabelIndex <= endLabelIndex);
            Debug.Assert(_sortedCaseLabels.Length > endLabelIndex);

            //  Start with empty stack of buckets.
            var switchBucketsStack = ArrayBuilder<SwitchBucket>.GetInstance();

            int curStartLabelIndex = startLabelIndex;

            //  While there are still labels
            while (curStartLabelIndex <= endLabelIndex)
            {
                SwitchBucket newBucket = CreateNextBucket(curStartLabelIndex, endLabelIndex);

                //      While the top bucket on stack can be merged with newBucket into a dense bucket
                //          merge top bucket on stack into newBucket, and pop bucket from stack
                //      End While

                while (!switchBucketsStack.IsEmpty())
                {
                    // get the bucket at top of the stack
                    SwitchBucket prevBucket = switchBucketsStack.Peek();
                    if (newBucket.TryMergeWith(prevBucket))
                    {
                        // pop the previous bucket from the stack
                        switchBucketsStack.Pop();
                    }
                    else
                    {
                        // merge failed
                        break;
                    }
                }

                //      Push newBucket on stack
                switchBucketsStack.Push(newBucket);

                // update curStartLabelIndex
                curStartLabelIndex++;
            }

            Debug.Assert(!switchBucketsStack.IsEmpty());

            // crumble leaf buckets into degenerate buckets where possible
            var crumbled = ArrayBuilder<SwitchBucket>.GetInstance();
            foreach (var uncrumbled in switchBucketsStack)
            {
                var degenerateSplit = uncrumbled.DegenerateBucketSplit;
                switch (degenerateSplit)
                {
                    case -1:
                        // cannot be split
                        crumbled.Add(uncrumbled);
                        break;

                    case 0:
                        // already degenerate
                        crumbled.Add(new SwitchBucket(_sortedCaseLabels, uncrumbled.StartLabelIndex, uncrumbled.EndLabelIndex, isDegenerate: true));
                        break;

                    default:
                        // can split
                        crumbled.Add(new SwitchBucket(_sortedCaseLabels, uncrumbled.StartLabelIndex, degenerateSplit - 1, isDegenerate: true));
                        crumbled.Add(new SwitchBucket(_sortedCaseLabels, degenerateSplit, uncrumbled.EndLabelIndex, isDegenerate: true));
                        break;
                }
            }

            switchBucketsStack.Free();
            return crumbled.ToImmutableAndFree();
        }

        private SwitchBucket CreateNextBucket(int startLabelIndex, int endLabelIndex)
        {
            Debug.Assert(startLabelIndex >= 0 && startLabelIndex <= endLabelIndex);
            return new SwitchBucket(_sortedCaseLabels, startLabelIndex);
        }

        #endregion

        #region "Switch bucket emit methods"

        private void EmitSwitchBucketsLinearLeaf(ImmutableArray<SwitchBucket> switchBuckets, int low, int high)
        {
            for (int i = low; i < high; i++)
            {
                var nextBucketLabel = new object();
                this.EmitSwitchBucket(switchBuckets[i], nextBucketLabel);

                //  nextBucketLabel:
                _builder.MarkLabel(nextBucketLabel);
            }

            this.EmitSwitchBucket(switchBuckets[high], _fallThroughLabel);
        }

        private void EmitSwitchBuckets(ImmutableArray<SwitchBucket> switchBuckets, int low, int high)
        {
            // if (high - low + 1 <= LinearSearchThreshold)
            if (high - low < LinearSearchThreshold)
            {
                this.EmitSwitchBucketsLinearLeaf(switchBuckets, low, high);
                return;
            }

            // This way (0 1 2 3) will produce a mid of 2 while
            // (0 1 2) will produce a mid of 1

            // Now, the first half is first to mid-1
            // and the second half is mid to last.
            int mid = (low + high + 1) / 2;

            object secondHalfLabel = new object();

            // Emit a conditional branch to the second half
            // before emitting the first half buckets.

            ConstantValue pivotConstant = switchBuckets[mid - 1].EndConstant;

            //  if(key > midLabelConstant)
            //      goto secondHalfLabel;
            this.EmitCondBranchForSwitch(
                _keyTypeCode.IsUnsigned() ? ILOpCode.Bgt_un : ILOpCode.Bgt,
                pivotConstant,
                secondHalfLabel);

            // Emit first half
            this.EmitSwitchBuckets(switchBuckets, low, mid - 1);

            // NOTE:    Typically marking a synthetic label needs a hidden sequence point.
            // NOTE:    Otherwise if you step (F11) to this label debugger may highlight previous (lexically) statement.
            // NOTE:    We do not need a hidden point in this implementation since we do not interleave jump table
            // NOTE:    and cases so the "previous" statement will always be "switch".

            //  secondHalfLabel:
            _builder.MarkLabel(secondHalfLabel);

            // Emit second half
            this.EmitSwitchBuckets(switchBuckets, mid, high);
        }

        private void EmitSwitchBucket(SwitchBucket switchBucket, object bucketFallThroughLabel)
        {
            if (switchBucket.LabelsCount == 1)
            {
                var c = switchBucket[0];
                //  if(key == constant)
                //      goto caseLabel;
                ConstantValue constant = c.Key;
                object caseLabel = c.Value;
                this.EmitEqBranchForSwitch(constant, caseLabel);
            }
            else
            {
                if (switchBucket.IsDegenerate)
                {
                    EmitRangeCheckedBranch(switchBucket.StartConstant, switchBucket.EndConstant, switchBucket[0].Value);
                }
                else
                {
                    //  Emit key normalized to startConstant (i.e. key - startConstant)
                    this.EmitNormalizedSwitchKey(switchBucket.StartConstant, switchBucket.EndConstant, bucketFallThroughLabel);

                    // Create the labels array for emitting a switch instruction for the bucket
                    object[] labels = this.CreateBucketLabels(switchBucket);

                    //  switch (N, label1, label2... labelN)
                    // Emit the switch instruction
                    _builder.EmitSwitch(labels);
                }
            }

            //  goto fallThroughLabel;
            _builder.EmitBranch(ILOpCode.Br, bucketFallThroughLabel);
        }

        private object[] CreateBucketLabels(SwitchBucket switchBucket)
        {
            //  switch (N, t1, t2... tN)
            //      IL ==> ILOpCode.Switch < unsigned int32 > < int32 >... < int32 >

            //  For example: given a switch bucket [1, 3, 5] and FallThrough Label,
            //  we create the following labels array:
            //  { CaseLabel1, FallThrough, CaseLabel3, FallThrough, CaseLabel5 }

            ConstantValue startConstant = switchBucket.StartConstant;
            bool hasNegativeCaseLabels = startConstant.IsNegativeNumeric;

            int nextCaseIndex = 0;
            ulong nextCaseLabelNormalizedValue = 0;

            ulong bucketSize = switchBucket.BucketSize;
            object[] labels = new object[bucketSize];

            for (ulong i = 0; i < bucketSize; ++i)
            {
                if (i == nextCaseLabelNormalizedValue)
                {
                    labels[i] = switchBucket[nextCaseIndex].Value;
                    nextCaseIndex++;

                    if (nextCaseIndex >= switchBucket.LabelsCount)
                    {
                        Debug.Assert(i == bucketSize - 1);
                        break;
                    }

                    ConstantValue caseLabelConstant = switchBucket[nextCaseIndex].Key;
                    if (hasNegativeCaseLabels)
                    {
                        var nextCaseLabelValue = caseLabelConstant.Int64Value;
                        Debug.Assert(nextCaseLabelValue > startConstant.Int64Value);
                        nextCaseLabelNormalizedValue = (ulong)(nextCaseLabelValue - startConstant.Int64Value);
                    }
                    else
                    {
                        var nextCaseLabelValue = caseLabelConstant.UInt64Value;
                        Debug.Assert(nextCaseLabelValue > startConstant.UInt64Value);
                        nextCaseLabelNormalizedValue = nextCaseLabelValue - startConstant.UInt64Value;
                    }

                    continue;
                }

                labels[i] = _fallThroughLabel;
            }

            Debug.Assert(nextCaseIndex >= switchBucket.LabelsCount);
            return labels;
        }

        #endregion

        #region "Helper emit methods"

        private void EmitCondBranchForSwitch(ILOpCode branchCode, ConstantValue constant, object targetLabel)
        {
            Debug.Assert(branchCode.IsBranch());
            RoslynDebug.Assert(constant != null &&
                SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(constant));
            RoslynDebug.Assert(targetLabel != null);

            // ldloc key
            // ldc constant
            // branch branchCode targetLabel

            _builder.EmitLoad(_key);
            _builder.EmitConstantValue(constant);
            _builder.EmitBranch(branchCode, targetLabel, GetReverseBranchCode(branchCode));
        }

        private void EmitEqBranchForSwitch(ConstantValue constant, object targetLabel)
        {
            RoslynDebug.Assert(constant != null &&
                SwitchConstantValueHelper.IsValidSwitchCaseLabelConstant(constant));
            RoslynDebug.Assert(targetLabel != null);

            _builder.EmitLoad(_key);

            if (constant.IsDefaultValue)
            {
                // ldloc key
                // brfalse targetLabel
                _builder.EmitBranch(ILOpCode.Brfalse, targetLabel);
            }
            else
            {
                _builder.EmitConstantValue(constant);
                _builder.EmitBranch(ILOpCode.Beq, targetLabel);
            }
        }

        private void EmitRangeCheckedBranch(ConstantValue startConstant, ConstantValue endConstant, object targetLabel)
        {
            _builder.EmitLoad(_key);

            // Normalize the key to 0 if needed

            // Emit:    ldc constant
            //          sub
            if (!startConstant.IsDefaultValue)
            {
                _builder.EmitConstantValue(startConstant);
                _builder.EmitOpCode(ILOpCode.Sub);
            }

            if (_keyTypeCode.Is64BitIntegral())
            {
                _builder.EmitLongConstant(endConstant.Int64Value - startConstant.Int64Value);
            }
            else
            {
                int Int32Value(ConstantValue value)
                {
                    // ConstantValue does not correctly convert byte and ushort values to int.
                    // It sign extends them rather than padding them. We compensate for that here.
                    // See also https://github.com/dotnet/roslyn/issues/18579
                    switch (value.Discriminator)
                    {
                        case ConstantValueTypeDiscriminator.Byte: return value.ByteValue;
                        case ConstantValueTypeDiscriminator.UInt16: return value.UInt16Value;
                        default: return value.Int32Value;
                    }
                }

                _builder.EmitIntConstant(Int32Value(endConstant) - Int32Value(startConstant));
            }

            _builder.EmitBranch(ILOpCode.Ble_un, targetLabel, ILOpCode.Bgt_un);
        }

        private static ILOpCode GetReverseBranchCode(ILOpCode branchCode)
        {
            switch (branchCode)
            {
                case ILOpCode.Beq:
                    return ILOpCode.Bne_un;

                case ILOpCode.Blt:
                    return ILOpCode.Bge;

                case ILOpCode.Blt_un:
                    return ILOpCode.Bge_un;

                case ILOpCode.Bgt:
                    return ILOpCode.Ble;

                case ILOpCode.Bgt_un:
                    return ILOpCode.Ble_un;

                default:
                    throw ExceptionUtilities.UnexpectedValue(branchCode);
            }
        }

        private void EmitNormalizedSwitchKey(ConstantValue startConstant, ConstantValue endConstant, object bucketFallThroughLabel)
        {
            _builder.EmitLoad(_key);

            // Normalize the key to 0 if needed

            // Emit:    ldc constant
            //          sub
            if (!startConstant.IsDefaultValue)
            {
                _builder.EmitConstantValue(startConstant);
                _builder.EmitOpCode(ILOpCode.Sub);
            }

            // range-check normalized value if needed
            EmitRangeCheckIfNeeded(startConstant, endConstant, bucketFallThroughLabel);

            // truncate key to 32bit
            _builder.EmitNumericConversion(_keyTypeCode, Microsoft.Cci.PrimitiveTypeCode.UInt32, false);
        }

        private void EmitRangeCheckIfNeeded(ConstantValue startConstant, ConstantValue endConstant, object bucketFallThroughLabel)
        {
            // switch treats key as an unsigned int.
            // this ensures that normalization does not introduce [over|under]flows issues with 32bit or shorter keys.
            // 64bit values, however must be checked before 32bit truncation happens.
            if (_keyTypeCode.Is64BitIntegral())
            {
                // Dup(normalized);
                // if ((ulong)(normalized) > (ulong)(endConstant - startConstant)) 
                // {
                //      // not going to use it in the switch
                //      Pop(normalized);
                //      goto bucketFallThroughLabel;
                // }

                var inRangeLabel = new object();

                _builder.EmitOpCode(ILOpCode.Dup);
                _builder.EmitLongConstant(endConstant.Int64Value - startConstant.Int64Value);
                _builder.EmitBranch(ILOpCode.Ble_un, inRangeLabel, ILOpCode.Bgt_un);
                _builder.EmitOpCode(ILOpCode.Pop);
                _builder.EmitBranch(ILOpCode.Br, bucketFallThroughLabel);
                // If we get to inRangeLabel, we should have key on stack, adjust for that.
                // builder cannot infer this since it has not seen all branches, 
                // but it will verify that our Adjustment is valid when more branches are known.
                _builder.AdjustStack(+1);
                _builder.MarkLabel(inRangeLabel);
            }
        }
        #endregion
    }
}
