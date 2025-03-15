// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Unique flow capture Id across interprocedural flow graph.
    /// This type essentially wraps each <see cref="CaptureId"/>, which is unique for each control flow graph,
    /// with its owning <see cref="ControlFlowGraph"/>.
    /// </summary>
    public readonly struct InterproceduralCaptureId : IEquatable<InterproceduralCaptureId>
    {
        internal InterproceduralCaptureId(CaptureId captureId, ControlFlowGraph controlFlowGraph, bool isLValueFlowCapture)
        {
            Id = captureId;
            ControlFlowGraph = controlFlowGraph;
            IsLValueFlowCapture = isLValueFlowCapture;
        }

        public CaptureId Id { get; }
        public ControlFlowGraph ControlFlowGraph { get; }
        public bool IsLValueFlowCapture { get; }

        public bool Equals(InterproceduralCaptureId other)
            => Id.Equals(other.Id) && ControlFlowGraph == other.ControlFlowGraph;

        public override bool Equals(object obj)
            => obj is InterproceduralCaptureId id && Equals(id);

        public override int GetHashCode()
            => RoslynHashCode.Combine(Id.GetHashCode(), ControlFlowGraph.GetHashCode());

        public static bool operator ==(InterproceduralCaptureId left, InterproceduralCaptureId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InterproceduralCaptureId left, InterproceduralCaptureId right)
        {
            return !(left == right);
        }
    }
}
