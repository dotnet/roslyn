// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CodeGen
{
    // HashBucket used when emitting hash table based string switch.
    // Each hash bucket contains the list of "<string constant, label>" key-value pairs
    // having identical hash value.
    using HashBucket = List<KeyValuePair<ConstantValue, object>>;

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
        /// Delegate to compute string hash code.
        /// This piece is language-specific because VB treats "" and null as equal while C# does not.
        /// </summary>
        public delegate uint GetStringHashCode(string key);

        /// <summary>
        /// Delegate to emit string compare call
        /// </summary>
        private readonly EmitStringCompareAndBranch _emitStringCondBranchDelegate;

        /// <summary>
        /// Delegate to emit string hash
        /// </summary>
        private readonly GetStringHashCode _computeStringHashcodeDelegate;

        /// <summary>
        /// Local storing the key hash value, used for emitting hash table based string switch.
        /// </summary>
        private readonly LocalDefinition _keyHash;

        internal SwitchStringJumpTableEmitter(
            ILBuilder builder,
            LocalOrParameter key,
            KeyValuePair<ConstantValue, object>[] caseLabels,
            object fallThroughLabel,
            LocalDefinition keyHash,
            EmitStringCompareAndBranch emitStringCondBranchDelegate,
            GetStringHashCode computeStringHashcodeDelegate)
        {
            Debug.Assert(caseLabels.Length > 0);
            Debug.Assert(emitStringCondBranchDelegate != null);

            _builder = builder;
            _key = key;
            _caseLabels = caseLabels;
            _fallThroughLabel = fallThroughLabel;
            _keyHash = keyHash;
            _emitStringCondBranchDelegate = emitStringCondBranchDelegate;
            _computeStringHashcodeDelegate = computeStringHashcodeDelegate;
        }

        internal void EmitJumpTable()
        {
            Debug.Assert(_keyHash == null || ShouldGenerateHashTableSwitch(_caseLabels.Length));

            if (_keyHash != null)
            {
                EmitHashTableSwitch();
            }
            else
            {
                EmitNonHashTableSwitch(_caseLabels);
            }
        }

        private void EmitHashTableSwitch()
        {
            // Hash value for the key must have already been computed and loaded into keyHash
            Debug.Assert(_keyHash != null);

            // Compute hash value for each case label constant and store the hash buckets
            // into a dictionary indexed by hash value.
            Dictionary<uint, HashBucket> stringHashMap = ComputeStringHashMap(
                                                            _caseLabels,
                                                            _computeStringHashcodeDelegate);

            // Emit conditional jumps to hash buckets.
            // EmitHashBucketJumpTable returns a map from hashValues to hashBucketLabels.
            Dictionary<uint, object> hashBucketLabelsMap = EmitHashBucketJumpTable(stringHashMap);

            // Emit hash buckets
            foreach (var kvPair in stringHashMap)
            {
                // hashBucketLabel:
                //  Emit direct string comparisons for each case label in hash bucket

                _builder.MarkLabel(hashBucketLabelsMap[kvPair.Key]);

                HashBucket hashBucket = kvPair.Value;
                this.EmitNonHashTableSwitch(hashBucket.ToArray());
            }
        }

        // Emits conditional jumps to hash buckets, returning a map from hashValues to hashBucketLabels.
        private Dictionary<uint, object> EmitHashBucketJumpTable(Dictionary<uint, HashBucket> stringHashMap)
        {
            int count = stringHashMap.Count;
            var hashBucketLabelsMap = new Dictionary<uint, object>(count);
            var jumpTableLabels = new KeyValuePair<ConstantValue, object>[count];
            int i = 0;

            foreach (uint hashValue in stringHashMap.Keys)
            {
                ConstantValue hashConstant = ConstantValue.Create(hashValue);
                object hashBucketLabel = new object();

                jumpTableLabels[i] = new KeyValuePair<ConstantValue, object>(hashConstant, hashBucketLabel);
                hashBucketLabelsMap[hashValue] = hashBucketLabel;

                i++;
            }

            // Emit conditional jumps to hash buckets by using an integral switch jump table based on keyHash.
            var hashBucketJumpTableEmitter = new SwitchIntegralJumpTableEmitter(
                builder: _builder,
                caseLabels: jumpTableLabels,
                fallThroughLabel: _fallThroughLabel,
                keyTypeCode: Cci.PrimitiveTypeCode.UInt32,
                key: _keyHash);

            hashBucketJumpTableEmitter.EmitJumpTable();

            return hashBucketLabelsMap;
        }

        private void EmitNonHashTableSwitch(KeyValuePair<ConstantValue, object>[] labels)
        {
            // Direct string comparison for each case label
            foreach (var kvPair in labels)
            {
                this.EmitCondBranchForStringSwitch(kvPair.Key, kvPair.Value);
            }

            _builder.EmitBranch(ILOpCode.Br, _fallThroughLabel);
        }

        private void EmitCondBranchForStringSwitch(ConstantValue stringConstant, object targetLabel)
        {
            Debug.Assert(stringConstant != null &&
                (stringConstant.IsString || stringConstant.IsNull));
            Debug.Assert(targetLabel != null);

            _emitStringCondBranchDelegate(_key, stringConstant, targetLabel);
        }

        // Compute hash value for each case label constant and store the hash buckets
        // into a dictionary indexed by hash value.
        private static Dictionary<uint, HashBucket> ComputeStringHashMap(
            KeyValuePair<ConstantValue, object>[] caseLabels,
            GetStringHashCode computeStringHashcodeDelegate)
        {
            Debug.Assert(caseLabels != null);
            var stringHashMap = new Dictionary<uint, HashBucket>(caseLabels.Length);

            foreach (var kvPair in caseLabels)
            {
                ConstantValue stringConstant = kvPair.Key;
                Debug.Assert(stringConstant.IsNull || stringConstant.IsString);

                uint hash = computeStringHashcodeDelegate((string)stringConstant.Value);

                HashBucket bucket;
                if (!stringHashMap.TryGetValue(hash, out bucket))
                {
                    bucket = new HashBucket();
                    stringHashMap.Add(hash, bucket);
                }

                Debug.Assert(!bucket.Contains(kvPair));
                bucket.Add(kvPair);
            }

            return stringHashMap;
        }

        internal static bool ShouldGenerateHashTableSwitch(CommonPEModuleBuilder module, int labelsCount)
        {
            return module.SupportsPrivateImplClass && ShouldGenerateHashTableSwitch(labelsCount);
        }

        private static bool ShouldGenerateHashTableSwitch(int labelsCount)
        {
            // Heuristic used by Dev10 compiler for emitting string switch:
            //  Generate hash table based string switch jump table
            //  if we have at least 7 case labels. Otherwise emit
            //  direct string comparisons with each case label constant.

            return labelsCount >= 7;
        }
    }
}
