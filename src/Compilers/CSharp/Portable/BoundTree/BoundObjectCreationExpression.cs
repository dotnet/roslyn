// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundObjectCreationExpression
    {
        public BoundObjectCreationExpression(SyntaxNode syntax, MethodSymbol constructor, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNamesOpt,
                                             ImmutableArray<RefKind> argumentRefKindsOpt, bool expanded, ImmutableArray<int> argsToParamsOpt, ConstantValue? constantValueOpt,
                                             BoundObjectInitializerExpressionBase? initializerExpressionOpt, Binder? binderOpt, TypeSymbol type, bool hasErrors = false)
            : this(syntax, constructor, ImmutableArray<MethodSymbol>.Empty, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, constantValueOpt, initializerExpressionOpt, wasTargetTyped: false, binderOpt, type, hasErrors)
        { }

        public BoundObjectCreationExpression Update(MethodSymbol constructor, ImmutableArray<BoundExpression> arguments, ImmutableArray<string> argumentNamesOpt, ImmutableArray<RefKind> argumentRefKindsOpt, bool expanded,
                                                    ImmutableArray<int> argsToParamsOpt, ConstantValue? constantValueOpt, BoundObjectInitializerExpressionBase? initializerExpressionOpt, Binder? binderOpt, TypeSymbol type)
        {
            return this.Update(constructor, ImmutableArray<MethodSymbol>.Empty, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, constantValueOpt, initializerExpressionOpt, this.WasTargetTyped, binderOpt, type);
        }

        public BoundObjectCreationExpression Update(MethodSymbol constructor, ImmutableArray<MethodSymbol> constructorsGroup, ImmutableArray<BoundExpression> arguments,
                                                    ImmutableArray<string> argumentNamesOpt, ImmutableArray<RefKind> argumentRefKindsOpt, bool expanded, ImmutableArray<int> argsToParamsOpt,
                                                    ConstantValue? constantValueOpt, BoundObjectInitializerExpressionBase? initializerExpressionOpt, Binder? binderOpt, TypeSymbol type)
        {
            return this.Update(constructor, constructorsGroup, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, constantValueOpt, initializerExpressionOpt, this.WasTargetTyped, binderOpt, type);
        }
    }
}
