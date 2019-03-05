// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(nullable-api): Doc Comment
    public readonly struct NullabilityInfo
    {
        public NullableAnnotation Annotation { get; }
        public NullableFlowState FlowState { get; }

        internal NullabilityInfo(NullableAnnotation annotation, NullableFlowState flowState)
        {
            Annotation = annotation;
            FlowState = flowState;
        }

        public override bool Equals(object obj) =>
            obj is NullabilityInfo info &&
                   Annotation == info.Annotation &&
                   FlowState == info.FlowState;

        public override int GetHashCode() =>
            Hash.Combine(Annotation.GetHashCode(), FlowState.GetHashCode());
    }
}
