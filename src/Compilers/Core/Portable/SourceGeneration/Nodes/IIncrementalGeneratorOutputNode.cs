﻿// Licensed to the .NET Foundation under one or more agreements.
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

    internal enum IncrementalGeneratorOutputKind
    {
        Source,
        PostInit
    }
}
