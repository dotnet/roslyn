// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Internal representation of an incremental output
    /// </summary>
    internal interface IIncrementalGeneratorOutputNode
    {
        IncrementalGeneratorOutputKind Kind { get; }

        void AppendOutputs(IncrementalExecutionContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents the various output kinds of an <see cref="IIncrementalGenerator"/>. 
    /// </summary>
    /// <remarks>
    /// Can be passed as a bit field when creating a <see cref="GeneratorDriver"/> to selectively disable outputs.
    /// </remarks>
    [Flags]
    public enum IncrementalGeneratorOutputKind
    {
        None = 0,
        Source = 0b1,
        PostInit = 0b10,
        NonSemantic = 0b100
    }
}
