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
    internal sealed class MethodArgumentInfo
    {
        public readonly MethodSymbol Method;
        public readonly ImmutableArray<BoundExpression> Arguments;
        public readonly BitVector DefaultArguments;
        public readonly bool Expanded;

        public MethodArgumentInfo(
            MethodSymbol method,
            ImmutableArray<BoundExpression> arguments,
            BitVector defaultArguments,
            bool expanded)
        {
            this.Method = method;
            this.Arguments = arguments;
            this.DefaultArguments = defaultArguments;
            this.Expanded = expanded;
        }

        public static MethodArgumentInfo CreateParameterlessMethod(MethodSymbol method)
        {
            Debug.Assert(method.ParameterCount == 0);
            return new MethodArgumentInfo(method, arguments: ImmutableArray<BoundExpression>.Empty, defaultArguments: default, expanded: false);
        }
    }
}
