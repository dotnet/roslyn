﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Emit
{
    public sealed class EmitDifferenceResult : EmitResult
    {
        public EmitBaseline Baseline { get; }

        internal EmitDifferenceResult(bool success, ImmutableArray<Diagnostic> diagnostics, EmitBaseline baseline) :
            base(success, diagnostics)
        {
            Baseline = baseline;
        }
    }
}
