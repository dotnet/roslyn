// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

        internal sealed override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes
        {
            get
            {
                var originalIndexes = this._underlyingParameter.InterpolatedStringHandlerArgumentIndexes;
                if (originalIndexes.IsDefaultOrEmpty)
                {
                    return originalIndexes;
                }

                // If this is the extension method receiver (ie, parameter 0), then any non-empty list of indexes must
                // be an error, and we should have already returned an empty list.
                Debug.Assert(_underlyingParameter.ContainingSymbol is not NamedTypeSymbol);
                Debug.Assert(originalIndexes.All(static index => index != BoundInterpolatedStringArgumentPlaceholder.InstanceParameter));
                return originalIndexes.SelectAsArray(static (index) => index switch
                {
                    BoundInterpolatedStringArgumentPlaceholder.InstanceParameter => throw ExceptionUtilities.Unreachable(),
                    BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver => 0,
                    BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter => BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter,
                    BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter => BoundInterpolatedStringArgumentPlaceholder.UnspecifiedParameter,
                    _ => index + 1
                });
            }
        }

        internal sealed override bool HasInterpolatedStringHandlerArgumentError => _underlyingParameter.HasInterpolatedStringHandlerArgumentError;
    }
}
