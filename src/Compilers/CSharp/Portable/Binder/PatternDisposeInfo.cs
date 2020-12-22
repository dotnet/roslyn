// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Information to be deduced while binding a foreach loop so that the loop can be lowered
    /// to a while over an enumerator.  Not applicable to the array or string forms.
    /// </summary>
    internal sealed record PatternDisposeInfo(
        MethodSymbol DisposeMethod,
        ImmutableArray<BoundExpression> Arguments,
        ImmutableArray<int> ArgsToParamsOpt,
        BitVector DefaultArguments);
}
