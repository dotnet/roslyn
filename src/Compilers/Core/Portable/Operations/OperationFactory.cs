// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal static class OperationFactory
    {
        public static IInvalidOperation CreateInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ImmutableArray<IOperation> children, bool isImplicit)
        {
            return new InvalidOperation(children, semanticModel, syntax, type: null, constantValue: null, isImplicit: isImplicit);
        }

        public static readonly IConvertibleConversion IdentityConversion = new IdentityConvertibleConversion();

        private class IdentityConvertibleConversion : IConvertibleConversion
        {
            public CommonConversion ToCommonConversion() => new CommonConversion(exists: true, isIdentity: true, isNumeric: false, isReference: false, methodSymbol: null, constrainedToType: null, isImplicit: true, isNullable: false);
        }
    }
}
