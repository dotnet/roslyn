// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Capture Id is an opaque identifier to represent an intermediate result from an <see cref="IFlowCaptureOperation"/>.
    /// </summary>
    public readonly struct CaptureId : IEquatable<CaptureId>
    {
        internal CaptureId(int value)
        {
            Debug.Assert(value >= 0);
            Value = value;
        }

        internal int Value { get; }

        /// <summary>
        /// Compares <see cref="CaptureId"/>s.
        /// </summary>
        public bool Equals(CaptureId other) => Value == other.Value;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is CaptureId && Equals((CaptureId)obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();
    }
}

