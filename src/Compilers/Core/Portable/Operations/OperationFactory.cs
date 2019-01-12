// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    internal static class OperationFactory
    {
        public static IInvalidOperation CreateInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ImmutableArray<IOperation> children, bool isImplicit)
        {
            return new InvalidOperation(children, semanticModel, syntax, type: null, constantValue: default(Optional<object>), isImplicit: isImplicit);
        }
    }
}
