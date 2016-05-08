// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

#if !SRM
using Microsoft.CodeAnalysis.CodeGen;
#endif

#if SRM
namespace System.Reflection.Metadata.Ecma335.Blobs
#else
namespace Roslyn.Reflection.Metadata.Ecma335.Blobs
#endif
{
#if SRM
    public
#endif
    sealed class BranchBuilder
    {
        // internal for testing:
        internal struct BranchInfo
        {
            internal readonly int ILOffset;
            internal readonly LabelHandle Label;
            internal readonly byte ShortOpCode;

            internal BranchInfo(int ilOffset, LabelHandle label, byte shortOpCode)
            {
                ILOffset = ilOffset;
                Label = label;
                ShortOpCode = shortOpCode;
            }

            internal bool IsShortBranchDistance(ImmutableArray<int>.Builder labels, out int distance)
            {
                const int shortBranchSize = 2;
                const int longBranchSize = 5;

                int labelTargetOffset = labels[Label.Id - 1];

                distance = labelTargetOffset - (ILOffset + shortBranchSize);
                if (unchecked((sbyte)distance) == distance)
                {
                    return true;
                }

                distance = labelTargetOffset - (ILOffset + longBranchSize);
                return false;
            }
        }

        private readonly ImmutableArray<BranchInfo>.Builder _branches;
        private readonly ImmutableArray<int>.Builder _labels;

        public BranchBuilder()
        {
            _branches = ImmutableArray.CreateBuilder<BranchInfo>();
            _labels = ImmutableArray.CreateBuilder<int>();
        }

        internal void Clear()
        {
            _branches.Clear();
            _labels.Clear();
        }

        internal LabelHandle AddLabel()
        {
            _labels.Add(-1);
            return new LabelHandle(_labels.Count);
        }

        internal void AddBranch(int ilOffset, LabelHandle label, byte shortOpCode)
        {
            Debug.Assert(ilOffset >= 0);
            Debug.Assert(_branches.Count == 0 || ilOffset > _branches.Last().ILOffset);
            ValidateLabel(label);
            _branches.Add(new BranchInfo(ilOffset, label, shortOpCode));
        }

        internal void MarkLabel(int ilOffset, LabelHandle label)
        {
            Debug.Assert(ilOffset >= 0);
            ValidateLabel(label);
            _labels[label.Id - 1] = ilOffset;
        }

        private void ValidateLabel(LabelHandle label)
        {
            if (label.IsNil)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (label.Id > _labels.Count)
            {
                // TODO: localize
                throw new ArgumentException("Label not defined", nameof(label));
            }
        }

        // internal for testing:
        internal IEnumerable<BranchInfo> Branches => _branches;

        // internal for testing:
        internal IEnumerable<int> Labels => _labels;

        internal int BranchCount => _branches.Count;

        internal void FixupBranches(BlobBuilder srcBuilder, BlobBuilder dstBuilder)
        {
            int srcOffset = 0;
            var branch = _branches[0];
            int branchIndex = 0;
            int blobOffset = 0;
            foreach (Blob blob in srcBuilder.GetBlobs())
            {
                Debug.Assert(blobOffset == 0 || blobOffset == 1 && blob.Buffer[blobOffset - 1] == 0xff);

                while (true)
                {
                    // copy bytes preceding the next branch, or till the end of the blob:
                    int chunkSize = Math.Min(branch.ILOffset - srcOffset, blob.Length - blobOffset);
                    dstBuilder.WriteBytes(blob.Buffer, blobOffset, chunkSize);
                    srcOffset += chunkSize;
                    blobOffset += chunkSize;

                    // there is no branch left in the blob:
                    if (blobOffset == blob.Length)
                    {
                        blobOffset = 0;
                        break;
                    }

                    Debug.Assert(blob.Buffer[blobOffset] == branch.ShortOpCode && (blobOffset + 1 == blob.Length || blob.Buffer[blobOffset + 1] == 0xff));
                    srcOffset += sizeof(byte) + sizeof(sbyte);

                    // write actual branch instruction:
                    int branchDistance;
                    if (branch.IsShortBranchDistance(_labels, out branchDistance))
                    {
                        dstBuilder.WriteByte(branch.ShortOpCode);
                        dstBuilder.WriteSByte((sbyte)branchDistance);
                    }
                    else
                    {
                        dstBuilder.WriteByte((byte)((ILOpCode)branch.ShortOpCode).GetLongBranch());
                        dstBuilder.WriteInt32(branchDistance);
                    }

                    // next branch:
                    branchIndex++;
                    if (branchIndex == _branches.Count)
                    {
                        branch = new BranchInfo(int.MaxValue, default(LabelHandle), 0);
                    }
                    else
                    {
                        branch = _branches[branchIndex];
                    }

                    // the branch starts at the very end and its operand is in the next blob:
                    if (blobOffset == blob.Length - 1)
                    {
                        blobOffset = 1;
                        break;
                    }

                    // skip fake branch instruction:
                    blobOffset += sizeof(byte) + sizeof(sbyte);
                }
            }
        }
    }
}
