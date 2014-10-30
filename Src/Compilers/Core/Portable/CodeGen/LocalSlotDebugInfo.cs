// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal struct LocalSlotDebugInfo
    {
        public readonly SynthesizedLocalKind SynthesizedKind;
        public readonly LocalDebugId Id;

        public LocalSlotDebugInfo(SynthesizedLocalKind synthesizedKind, LocalDebugId id)
        {
            this.SynthesizedKind = synthesizedKind;
            this.Id = id;
        }
    }
}
