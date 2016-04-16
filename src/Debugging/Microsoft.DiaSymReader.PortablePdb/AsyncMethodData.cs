// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class AsyncMethodData
    {
        public static readonly AsyncMethodData None = new AsyncMethodData();

        public readonly MethodDefinitionHandle KickoffMethod;
        public readonly int CatchHandlerOffset;
        public readonly ImmutableArray<int> YieldOffsets;
        public readonly ImmutableArray<int> ResumeOffsets;
        public readonly ImmutableArray<int> ResumeMethods;

        private AsyncMethodData()
        {
        }

        public AsyncMethodData(
            MethodDefinitionHandle kickoffMethod,
            int catchHandlerOffset,
            ImmutableArray<int> yieldOffsets,
            ImmutableArray<int> resumeOffsets,
            ImmutableArray<int> resumeMethods)
        {
            Debug.Assert(!kickoffMethod.IsNil);
            Debug.Assert(catchHandlerOffset >= -1);

            Debug.Assert(yieldOffsets.Length == resumeOffsets.Length);
            Debug.Assert(yieldOffsets.Length == resumeMethods.Length);

            KickoffMethod = kickoffMethod;
            CatchHandlerOffset = catchHandlerOffset;
            YieldOffsets = yieldOffsets;
            ResumeOffsets = resumeOffsets;
            ResumeMethods = resumeMethods;
        }

        public bool IsNone => ReferenceEquals(this, None);
    }
}
