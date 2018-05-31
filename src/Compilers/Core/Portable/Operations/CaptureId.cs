// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add doc comments.
    /// </summary>
    public struct CaptureId : IEquatable<CaptureId>
    {
        internal CaptureId(int id)
        {
            Id = id;
        }

        // PROTOTYPE(dataflow): Make this a private readonly field once we remove all uses in ControlFlowGraphBuilder.
        internal int Id { get; }

        public bool Equals(CaptureId other) => Id == other.Id;

        public override bool Equals(object obj) => obj is CaptureId && Equals((CaptureId)obj);

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => Id.ToString();
    }
}

