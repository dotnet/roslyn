// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public enum BranchKind
    {
        None = 0x0,
        Continue = 0x1,
        Break = 0x2,
        GoTo = 0x3
    }
}

