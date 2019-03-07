// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(nullable-api): Doc Comment
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public readonly struct NullabilityInfo
    {
        public NullableAnnotation Annotation { get; }
        public NullableFlowState FlowState { get; }

        internal NullabilityInfo(NullableAnnotation annotation, NullableFlowState flowState)
        {
            Annotation = annotation switch
            {
                NullableAnnotation.Nullable => NullableAnnotation.Annotated,
                NullableAnnotation.NotNullable => NullableAnnotation.NotAnnotated,
                _ => annotation
            };
            FlowState = flowState;
        }

        private string GetDebuggerDisplay() => $"{{Annotation: {Annotation}, Flow State: {FlowState}}}";

        public override bool Equals(object obj) =>
            obj is NullabilityInfo info &&
                   Annotation == info.Annotation &&
                   FlowState == info.FlowState;

        public override int GetHashCode() =>
            Hash.Combine(Annotation.GetHashCode(), FlowState.GetHashCode());

    }
}
