// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    // HashBucket used when emitting hash table based string switch.
    // Each hash bucket contains the list of "<string constant, label>" key-value pairs
    // having identical hash value.
    using Bucket = List<KeyValuePair<ConstantValue, object>>;

    internal struct SwitchStringJumpTableEmitter
    {
        private readonly ILBuilder _builder;

        /// <summary>
        /// Switch key for the jump table
        /// </summary>
        private readonly LocalOrParameter _key;

        /// <summary>
        /// Switch case labels
        /// </summary>
        private readonly KeyValuePair<ConstantValue, object>[] _caseLabels;

        /// <summary>
        /// Fall through label for the jump table
        /// </summary>
        private readonly object _fallThroughLabel;

        /// <summary>
        /// Delegate to emit string compare call and conditional branch based on the compare result.
        /// </summary>
        /// <param name="key">Key to compare</param>
        /// <param name="stringConstant">Case constant to compare the key against</param>
        /// <param name="targetLabel">Target label to branch to if key = stringConstant</param>
        public delegate void EmitStringCompareAndBranch(LocalOrParameter key, ConstantValue stringConstant, object targetLabel);

        /// <summary>
        /// Delegate to emit string compare call
        /// </summary>
        private readonly EmitStringCompareAndBranch _emitStringCondBranchDelegate;

        private readonly Func<LocalDefinition>? _emitStoreKeyLength;
        private readonly Func<int, LocalDefinition>? _emitStoreCharAtIndex;
        private readonly Action<int>? _emitPushCharAtIndex;
        private readonly Action? _emitPushKeyLength;

        internal SwitchStringJumpTableEmitter(
            ILBuilder builder,
            LocalOrParameter key,
            KeyValuePair<ConstantValue, object>[] caseLabels,
            object fallThroughLabel,
            EmitStringCompareAndBranch emitStringCondBranchDelegate,
            Action? emitPushKeyLength,
            Func<LocalDefinition>? emitStoreKeyLength,
            Action<int>? emitPushCharAtIndex,
            Func<int, LocalDefinition>? emitStoreCharAtIndex)
        {
            Debug.Assert(caseLabels.Length > 0);
            RoslynDebug.Assert(emitStringCondBranchDelegate != null);

            _builder = builder;
            _key = key;
            _caseLabels = caseLabels;
            _fallThroughLabel = fallThroughLabel;
            _emitStringCondBranchDelegate = emitStringCondBranchDelegate;
            _emitStoreKeyLength = emitStoreKeyLength;
            _emitStoreCharAtIndex = emitStoreCharAtIndex;
            _emitPushCharAtIndex = emitPushCharAtIndex;
            _emitPushKeyLength = emitPushKeyLength;
        }

        internal void EmitJumpTable()
        {
            if (_emitStoreKeyLength != null)
            {
                Debug.Assert(_emitStoreKeyLength != null && _emitStoreCharAtIndex != null && _emitPushCharAtIndex != null && _emitPushKeyLength != null);
                EmitTrieSwitch();
            }
            else
            {
                EmitNonTrieSwitch(_caseLabels);
            }
        }

        private void EmitTrieSwitch()
        {
            var (groupedByLength, nullCase) = GroupByLength(_caseLabels);
            _emitStringCondBranchDelegate(_key, ConstantValue.Null, nullCase ?? _fallThroughLabel);

            if (groupedByLength.Count == 1)
            {
                Debug.Assert(_emitPushKeyLength != null);
                _emitPushKeyLength();
                var (length, bucket) = groupedByLength.First();
                _builder.EmitConstantValue(ConstantValue.Create(length));
                _builder.EmitBranch(ILOpCode.Bne_un, _fallThroughLabel);
                if (length == 0)
                {
                    Debug.Assert(bucket.Count == 1);
                    _builder.EmitBranch(ILOpCode.Br, bucket[0].Key);
                }
                else
                {
                    EmitTrieSwitchPart(bucket, 0, length);
                }
                return;
            }

            var (lengthBucketLabelsMap, keyLengthTemp) = EmitLengthBucketJumpTable(groupedByLength);

            // Emit buckets
            foreach (var (length, bucket) in groupedByLength)
            {
                if (length != 0)
                {
                    _builder.MarkLabel(lengthBucketLabelsMap[length]);
                    EmitTrieSwitchPart(bucket, 0, length);
                }
            }

            FreeTemp(keyLengthTemp);
        }

        private static (Dictionary<int, Bucket> grouped, object? nullCase) GroupByLength(KeyValuePair<ConstantValue, object>[] caseLabels)
        {
            object? nullCase = null;
            var dictionary = new Dictionary<int, Bucket>();
            foreach (var (constantValue, label) in caseLabels)
            {
                Debug.Assert(constantValue.IsNull || constantValue.IsString);
                if (constantValue.IsNull)
                {
                    nullCase = label;
                }
                var length = ((string)constantValue.Value!).Length;
                if (!dictionary.TryGetValue(length, out var bucket))
                {
                    bucket = dictionary[length] = new Bucket();
                }

                bucket.Add(new KeyValuePair<ConstantValue, object>(constantValue, label));
            }
            return (dictionary, nullCase);
        }

        private (Dictionary<int, object> labelsMap, LocalDefinition? temp) EmitLengthBucketJumpTable(Dictionary<int, Bucket> groupedByLength)
        {
            int count = groupedByLength.Count;
            var lengthBucketLabelsMap = new Dictionary<int, object>(count);
            var jumpTableLabels = new KeyValuePair<ConstantValue, object>[count];
            int i = 0;

            foreach (var (length, bucket) in groupedByLength)
            {
                object bucketLabel;
                if (length == 0)
                {
                    Debug.Assert(bucket.Count == 1);
                    bucketLabel = bucket[0].Value;
                }
                else
                {
                    bucketLabel = new object();
                    lengthBucketLabelsMap[length] = bucketLabel;
                }

                var lengthConstant = ConstantValue.Create(length);
                jumpTableLabels[i] = new KeyValuePair<ConstantValue, object>(lengthConstant, bucketLabel);

                i++;
            }

            Debug.Assert(_emitStoreKeyLength != null);
            var keyLength = _emitStoreKeyLength();

            // Emit conditional jumps to buckets by using an integral switch jump table based on keyLength.
            var lengthBucketJumpTableEmitter = new SwitchIntegralJumpTableEmitter(
                builder: _builder,
                caseLabels: jumpTableLabels,
                fallThroughLabel: _fallThroughLabel,
                keyTypeCode: Cci.PrimitiveTypeCode.Int32,
                key: keyLength);

            lengthBucketJumpTableEmitter.EmitJumpTable();

            return (lengthBucketLabelsMap, keyLength);
        }

        private void EmitTrieSwitchPart(Bucket bucket, int charIndex, int length)
        {
            var groupedByChar = GroupByChar(bucket, charIndex);

            if (groupedByChar.Count == 1)
            {
                Debug.Assert(_emitPushCharAtIndex != null);
                _emitPushCharAtIndex(charIndex);
                var (key, subBucket) = groupedByChar.First();
                _builder.EmitConstantValue(ConstantValue.Create((int)key));
                _builder.EmitBranch(ILOpCode.Bne_un, _fallThroughLabel);
                if (charIndex == length - 1)
                {
                    Debug.Assert(subBucket.Count == 1);
                    _builder.EmitBranch(ILOpCode.Br, subBucket[0].Value);
                }
                else
                {
                    EmitTrieSwitchPart(subBucket, charIndex + 1, length);
                }
                return;
            }

            var (charBucketLabelsMap, charTemp) = EmitCharJumpTable(groupedByChar, charIndex, length);

            if (charIndex == length - 1)
                return;

            Debug.Assert(charBucketLabelsMap != null);
            foreach (var (key, subBucket) in groupedByChar)
            {
                _builder.MarkLabel(charBucketLabelsMap[key]);

                EmitTrieSwitchPart(subBucket, charIndex + 1, length);
            }

            FreeTemp(charTemp);
        }

        private static Dictionary<char, Bucket> GroupByChar(Bucket caseLabels, int charIndex)
        {
            var dictionary = new Dictionary<char, Bucket>();
            foreach (var (constantValue, label) in caseLabels)
            {
                Debug.Assert(constantValue.IsString);
                var character = ((string)constantValue.Value!)[charIndex];
                if (!dictionary.TryGetValue(character, out var bucket))
                {
                    bucket = dictionary[character] = new Bucket();
                }

                bucket.Add(new KeyValuePair<ConstantValue, object>(constantValue, label));
            }
            return dictionary;
        }

        private (Dictionary<char, object>? labelsMap, LocalDefinition?) EmitCharJumpTable(Dictionary<char, Bucket> groupedByChar, int charIndex, int length)
        {
            int count = groupedByChar.Count;

            bool finalChar = charIndex == length - 1;
            var charBucketLabelsMap = finalChar ? null : new Dictionary<char, object>(count);
            var jumpTableLabels = new KeyValuePair<ConstantValue, object>[count];
            int i = 0;

            foreach (var (key, bucket) in groupedByChar)
            {
                object bucketLabel;
                if (finalChar)
                {
                    Debug.Assert(bucket.Count == 1);
                    bucketLabel = bucket[0].Value;
                }
                else
                {
                    bucketLabel = new object();
                    charBucketLabelsMap![key] = bucketLabel;
                }

                ConstantValue charConstant = ConstantValue.Create((short)key);
                jumpTableLabels[i] = new KeyValuePair<ConstantValue, object>(charConstant, bucketLabel);

                i++;
            }

            Debug.Assert(_emitStoreCharAtIndex != null);

            var charTemp = _emitStoreCharAtIndex(charIndex);
            // Emit conditional jumps to hash buckets by using an integral switch jump table based on keyHash.
            var charBucketJumpTableEmitter = new SwitchIntegralJumpTableEmitter(
                builder: _builder,
                caseLabels: jumpTableLabels,
                fallThroughLabel: _fallThroughLabel,
                keyTypeCode: Cci.PrimitiveTypeCode.Char,
                key: charTemp);

            charBucketJumpTableEmitter.EmitJumpTable();

            return (charBucketLabelsMap, charTemp);
        }

        private void FreeTemp(LocalDefinition? temp)
        {
            if (temp != null)
            {
                _builder.LocalSlotManager.FreeSlot(temp);
            }
        }

        private void EmitNonTrieSwitch(KeyValuePair<ConstantValue, object>[] labels)
        {
            // Direct string comparison for each case label
            foreach (var kvPair in labels)
            {
                EmitCondBranchForStringSwitch(kvPair.Key, kvPair.Value);
            }

            _builder.EmitBranch(ILOpCode.Br, _fallThroughLabel);
        }

        private void EmitCondBranchForStringSwitch(ConstantValue stringConstant, object targetLabel)
        {
            RoslynDebug.Assert(stringConstant != null &&
                (stringConstant.IsString || stringConstant.IsNull));
            RoslynDebug.Assert(targetLabel != null);

            _emitStringCondBranchDelegate(_key, stringConstant, targetLabel);
        }

        internal static bool ShouldGenerateTrieSwitch(int labelsCount)
        {
            // Heuristic used for emitting string switch:
            //  Generate trie based string switch jump table
            //  if we have at least 3 case labels. Otherwise emit
            //  direct string comparisons with each case label constant.

            return labelsCount >= 3;
        }
    }
}
