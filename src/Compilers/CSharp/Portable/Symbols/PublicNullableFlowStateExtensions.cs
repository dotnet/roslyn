// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class PublicNullableFlowStateExtensions
    {
        public static NullableFlowState ToInternalFlowState(this CodeAnalysis.NullableFlowState flowState)
        {
            Debug.Assert((CodeAnalysis.NullableFlowState)(NullableFlowState.NotNull + 1) == CodeAnalysis.NullableFlowState.NotNull);
            return flowState switch
            {
                CodeAnalysis.NullableFlowState.Default => NullableFlowState.NotNull,
                CodeAnalysis.NullableFlowState.NotNull => NullableFlowState.NotNull,
                CodeAnalysis.NullableFlowState.MaybeNull => NullableFlowState.MaybeNull,
                _ => throw ExceptionUtilities.UnexpectedValue(flowState)
            };
        }
    }
}
