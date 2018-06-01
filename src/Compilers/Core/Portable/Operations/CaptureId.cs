// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add doc comments.
    /// </summary>
    public struct CaptureId : IEquatable<CaptureId>
    {
        internal CaptureId(int value)
        {
            Value = value;
        }

        internal int Value { get; }

        public bool Equals(CaptureId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is CaptureId && Equals((CaptureId)obj);

        public override int GetHashCode() => Value.GetHashCode();
    }
}

