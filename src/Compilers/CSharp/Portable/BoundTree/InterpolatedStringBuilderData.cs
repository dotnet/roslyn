// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct InterpolatedStringBuilderData
    {
        public readonly TypeSymbol BuilderType;
        public readonly BoundCall? Construction;
        public readonly bool UsesBoolReturns;
        /// <summary>
        /// The scope of the expression that contained the interpolated string during initial binding. This is used to determine the SafeToEscape rules
        /// for the builder during lowering.
        /// </summary>
        public readonly uint ScopeOfContainingExpression;

        public InterpolatedStringBuilderData(TypeSymbol builderType, BoundCall? construction, bool usesBoolReturns, uint scopeOfContainingExpression)
        {
            BuilderType = builderType;
            Construction = construction;
            UsesBoolReturns = usesBoolReturns;
            ScopeOfContainingExpression = scopeOfContainingExpression;
        }
    }
}
