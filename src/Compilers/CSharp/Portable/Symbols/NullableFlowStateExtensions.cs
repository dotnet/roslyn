// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class NullableFlowStateExtensions
    {
        internal static CodeAnalysis.NullableFlowState ToPublicFlowState(this NullableFlowState nullableFlowState)
        {
            Debug.Assert((CodeAnalysis.NullableFlowState)(NullableFlowState.NotNull + 1) == CodeAnalysis.NullableFlowState.NotNull);
            return (CodeAnalysis.NullableFlowState)nullableFlowState + 1;
        }
    }
}
