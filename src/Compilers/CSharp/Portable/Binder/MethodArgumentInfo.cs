// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Information about the arguments of a call that can turned into a BoundCall later without recalculating
    /// default arguments.
    /// </summary>
    internal sealed record MethodArgumentInfo(
        MethodSymbol Method,
        ImmutableArray<BoundExpression> Arguments,
        ImmutableArray<int> ArgsToParamsOpt,
        BitVector DefaultArguments,
        bool Expanded)
    {
        public static MethodArgumentInfo CreateParameterlessMethod(MethodSymbol method)
        {
            Debug.Assert(method.ParameterCount == 0);
            return new MethodArgumentInfo(method, Arguments: ImmutableArray<BoundExpression>.Empty, ArgsToParamsOpt: default, DefaultArguments: default, Expanded: false);
        }
    }
}
