﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The nullable state of an rvalue computed in <see cref="NullableWalker"/>.
    /// When in doubt we conservatively use <see cref="NullableFlowState.NotNull"/>
    /// to minimize diagnostics.
    /// </summary>
    internal enum NullableFlowState : byte
    {
        NotNull,
        MaybeNull
    }
}
