// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Emit
{
    public sealed class EmitDifferenceResult : EmitResult
    {
        private readonly EmitBaseline baseline;

        internal EmitDifferenceResult(bool success, ImmutableArray<Diagnostic> diagnostics, EmitBaseline baseline) :
            base(success, diagnostics)
        {
            this.baseline = baseline;
        }

        public EmitBaseline Baseline
        {
            get { return this.baseline; }
        }
    }
}
