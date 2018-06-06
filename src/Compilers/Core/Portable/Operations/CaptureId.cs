// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Capture Id is an opaque identifier to represent an intermediate result from an <see cref="IFlowCaptureOperation"/>.
    /// </summary>
    public struct CaptureId : IEquatable<CaptureId>
    {
        internal CaptureId(int value)
        {
            Value = value;
        }

        internal int Value { get; }

        /// <summary>
        /// Compares <see cref="CaptureId"/>s.
        /// </summary>
        public bool Equals(CaptureId other) => Value == other.Value;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CaptureId && Equals((CaptureId)obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();
    }
}

