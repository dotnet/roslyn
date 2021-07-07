// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.Emit
{
    public sealed class EmitDifferenceResult : EmitResult
    {
        public EmitBaseline? Baseline { get; }

        public ImmutableArray<MethodDefinitionHandle> UpdatedMethods { get; }

        public ImmutableArray<TypeDefinitionHandle> UpdatedTypes { get; }

        internal EmitDifferenceResult(bool success, ImmutableArray<Diagnostic> diagnostics, EmitBaseline? baseline, ImmutableArray<MethodDefinitionHandle> updatedMethods, ImmutableArray<TypeDefinitionHandle> updatedTypes)
            : base(success, diagnostics)
        {
            Baseline = baseline;
            UpdatedMethods = updatedMethods;
            UpdatedTypes = updatedTypes;
        }
    }
}
