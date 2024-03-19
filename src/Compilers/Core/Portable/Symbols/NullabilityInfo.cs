// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public readonly struct NullabilityInfo : IEquatable<NullabilityInfo>
    {
        /// <summary>
        /// The nullable annotation of the expression represented by the syntax node. This represents
        /// the nullability of expressions that can be assigned to this expression, if this expression
        /// can be used as an lvalue.
        /// </summary>
        public NullableAnnotation Annotation { get; }

        /// <summary>
        /// The nullable flow state of the expression represented by the syntax node. This represents
        /// the compiler's understanding of whether this expression can currently contain null, if
        /// this expression can be used as an rvalue.
        /// </summary>
        public NullableFlowState FlowState { get; }

        internal NullabilityInfo(NullableAnnotation annotation, NullableFlowState flowState)
        {
            Annotation = annotation;
            FlowState = flowState;
        }

        private string GetDebuggerDisplay() => $"{{Annotation: {Annotation}, Flow State: {FlowState}}}";

        public override bool Equals(object? other) =>
            other is NullabilityInfo info && Equals(info);

        public override int GetHashCode() =>
            Hash.Combine(((int)Annotation).GetHashCode(), ((int)FlowState).GetHashCode());

        public bool Equals(NullabilityInfo other) =>
            Annotation == other.Annotation &&
            FlowState == other.FlowState;
    }
}
