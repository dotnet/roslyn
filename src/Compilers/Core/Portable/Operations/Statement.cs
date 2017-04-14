// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    internal partial class VariableDeclaration : IVariableDeclaration
    {
        public VariableDeclaration(ILocalSymbol variable, IOperation initialValue, SyntaxNode syntax) :
            this(variable,
                initialValue,
                variable == null || (initialValue != null && initialValue.IsInvalid),
                syntax,
                type: null,
                constantValue: default(Optional<object>))
        {
        }
    }
}