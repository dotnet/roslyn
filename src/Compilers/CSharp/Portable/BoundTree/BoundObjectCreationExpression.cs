// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundObjectCreationExpression
    {
        public BoundObjectCreationExpression(SyntaxNode syntax, MethodSymbol constructor, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNamesOpt,
                                             ImmutableArray<RefKind> argumentRefKindsOpt, bool expanded, ImmutableArray<int> argsToParamsOpt, ConstantValue constantValueOpt,
                                             BoundObjectInitializerExpressionBase initializerExpressionOpt, Binder binderOpt, TypeSymbol type, bool hasErrors = false)
            : this(syntax, constructor, ImmutableArray<MethodSymbol>.Empty, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, constantValueOpt, initializerExpressionOpt, binderOpt, type, hasErrors)
        { }

        public BoundObjectCreationExpression Update(MethodSymbol constructor, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNamesOpt, ImmutableArray<RefKind> argumentRefKindsOpt, bool expanded,
                                                    ImmutableArray<int> argsToParamsOpt, ConstantValue constantValueOpt, BoundObjectInitializerExpressionBase initializerExpressionOpt, Binder binderOpt, TypeSymbol type)
        {
            return this.Update(constructor, ImmutableArray<MethodSymbol>.Empty, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, constantValueOpt, initializerExpressionOpt, binderOpt, type);
        }
    }
}
