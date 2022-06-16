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

        /// <summary>
        /// Handles of methods with sequence points that have been updated in this delta.
        /// </summary>
        public ImmutableArray<MethodDefinitionHandle> UpdatedMethods { get; }

        /// <summary>
        /// Handles of types that were changed (updated or inserted) in this delta.
        /// </summary>
        public ImmutableArray<TypeDefinitionHandle> ChangedTypes { get; }

        internal EmitDifferenceResult(bool success, ImmutableArray<Diagnostic> diagnostics, EmitBaseline? baseline, ImmutableArray<MethodDefinitionHandle> updatedMethods, ImmutableArray<TypeDefinitionHandle> changedTypes)
            : base(success, diagnostics)
        {
            Baseline = baseline;
            UpdatedMethods = updatedMethods;
            ChangedTypes = changedTypes;
        }
    }
}
