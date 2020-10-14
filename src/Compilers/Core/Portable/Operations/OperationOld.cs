
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(iop): Delete this after migration
    internal abstract class OperationOld : Operation
    {
        protected OperationOld(OperationKind kind, SemanticModel? semanticModel, SyntaxNode syntax, ITypeSymbol type, ConstantValue constantValue, bool isImplicit)
            : base(semanticModel, syntax, isImplicit)
        {
            // Constant value cannot be "null" for non-nullable value type operations.
            Debug.Assert(type?.IsValueType != true || ITypeSymbolHelpers.IsNullableType(type) || constantValue == null || constantValue == CodeAnalysis.ConstantValue.Unset || !constantValue.IsNull);

            Type = type;
            Kind = kind;
            OperationConstantValue = constantValue;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override ITypeSymbol? Type { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override OperationKind Kind { get; }

        internal override CodeAnalysis.ConstantValue? OperationConstantValue { get; }
    }
}
