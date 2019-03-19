// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(nullable-api): Doc Comment
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public readonly struct NullabilityInfo : IEquatable<NullabilityInfo>
    {
        public NullableAnnotation Annotation { get; }
        public NullableFlowState FlowState { get; }

        internal NullabilityInfo(NullableAnnotation annotation, NullableFlowState flowState)
        {
            Annotation = annotation;
            FlowState = flowState;
        }

        private string GetDebuggerDisplay() => $"{{Annotation: {Annotation}, Flow State: {FlowState}}}";

        public override bool Equals(object other) =>
            other is NullabilityInfo info && Equals(info);

        public override int GetHashCode() =>
            Hash.Combine(Annotation.GetHashCode(), FlowState.GetHashCode());

        public bool Equals(NullabilityInfo other) =>
            Annotation == other.Annotation &&
            FlowState == other.FlowState;
    }
}
