// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kinds of synthetic local references.
    /// </summary>
    public enum SyntheticLocalKind
    {
        None = 0x0,

        /// <summary>
        /// Created to capture the step value of a VB for loop.
        /// </summary>
        ForLoopStepValue = 0x1,
        /// <summary>
        /// Created to capture the limit value of a VB for loop.
        /// </summary>
        ForLoopLimitValue = 0x2
    }
}

