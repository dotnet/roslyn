// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Emit
{
    public sealed class EmitDifferenceResult : EmitResult
    {
        private readonly EmitBaseline _baseline;

        internal EmitDifferenceResult(bool success, ImmutableArray<Diagnostic> diagnostics, EmitBaseline baseline) :
            base(success, diagnostics, entryPointOpt: null)
        {
            _baseline = baseline;
        }

        public EmitBaseline Baseline
        {
            get { return _baseline; }
        }
    }
}
