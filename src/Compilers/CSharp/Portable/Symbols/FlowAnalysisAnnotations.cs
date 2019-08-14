// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum FlowAnalysisAnnotations
    {
        None = 0,
        AllowNull = 1 << 0,
        DisallowNull = 1 << 1,
        MaybeNullWhenTrue = 1 << 2,
        MaybeNullWhenFalse = 1 << 3,
        MaybeNull = MaybeNullWhenTrue | MaybeNullWhenFalse,
        NotNullWhenTrue = 1 << 4,
        NotNullWhenFalse = 1 << 5,
        NotNull = NotNullWhenTrue | NotNullWhenFalse,
        DoesNotReturnIfFalse = 1 << 6,
        DoesNotReturnIfTrue = 1 << 7,
        DoesNotReturn = DoesNotReturnIfTrue | DoesNotReturnIfFalse,
    }
}
