// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class RewrittenParameterSymbol : WrappedParameterSymbol
    {
        public RewrittenParameterSymbol(ParameterSymbol originalParameter) :
            base(originalParameter)
        {
        }

        internal sealed override bool IsCallerLineNumber => _underlyingParameter.IsCallerLineNumber;

        internal sealed override bool IsCallerFilePath => _underlyingParameter.IsCallerFilePath;

        internal sealed override bool IsCallerMemberName => _underlyingParameter.IsCallerMemberName;

        internal sealed override int CallerArgumentExpressionParameterIndex => _underlyingParameter.CallerArgumentExpressionParameterIndex;

        internal sealed override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable(); // PROTOTYPE(roles): Follow up

        internal sealed override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable(); // PROTOTYPE(roles): Follow up
    }
}
