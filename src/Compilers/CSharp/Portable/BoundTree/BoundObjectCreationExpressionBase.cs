// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundObjectCreationExpressionBase
    {
        public abstract MethodSymbol? Constructor { get; }
        public abstract ImmutableArray<BoundExpression> Arguments { get; }
        public abstract ImmutableArray<string?> ArgumentNamesOpt { get; }
        public abstract ImmutableArray<RefKind> ArgumentRefKindsOpt { get; }
        public abstract bool Expanded { get; }
        public abstract ImmutableArray<int> ArgsToParamsOpt { get; }
        public abstract BitVector DefaultArguments { get; }
        public abstract BoundObjectInitializerExpressionBase? InitializerExpressionOpt { get; }
        public abstract bool WasTargetTyped { get; }
    }
}
