// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A description of a step of an incremental generator that was executed.
    /// </summary>
    public sealed class IncrementalGeneratorRunStep
    {
        internal IncrementalGeneratorRunStep(string? stepName, ImmutableArray<(IncrementalGeneratorRunStep Source, int OutputIndex)> inputs, ImmutableArray<(object Value, IncrementalStepRunReason OutputState)> outputs, TimeSpan elapsedTime)
        {
            Debug.Assert(!inputs.IsDefault);
            Debug.Assert(!outputs.IsDefault);
            Name = stepName;
            Inputs = inputs;
            Outputs = outputs;
            ElapsedTime = elapsedTime;
        }

        public string? Name { get; }
        public ImmutableArray<(IncrementalGeneratorRunStep Source, int OutputIndex)> Inputs { get; }
        public ImmutableArray<(object Value, IncrementalStepRunReason Reason)> Outputs { get; }
        public TimeSpan ElapsedTime { get; }
    }
}
