// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
