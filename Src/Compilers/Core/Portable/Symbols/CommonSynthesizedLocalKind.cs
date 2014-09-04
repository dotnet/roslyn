// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal enum CommonSynthesizedLocalKind : short
    {
        /// <summary>
        /// Temp variable created by the emitter.
        /// </summary>
        EmitterTemp = -4,

        /// <summary>
        /// Temp variable created by the optimizer.
        /// </summary>
        OptimizerTemp = -3,

        /// <summary>
        /// Temp variable created during lowering.
        /// </summary>
        LoweringTemp = -2,

        /// <summary>
        /// The variable is not synthesized.
        /// </summary>
        None = -1
    }
}
