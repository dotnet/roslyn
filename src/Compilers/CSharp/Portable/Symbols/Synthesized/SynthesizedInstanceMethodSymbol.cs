// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A base class for synthesized methods that want a this parameter.
    /// </summary>
    internal abstract class SynthesizedInstanceMethodSymbol : SynthesizedMethodSymbol
    {
        private ParameterSymbol _lazyThisParameter;

        // PROTOTYPE should be sealed
        public override bool IsStatic => false;

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            if ((object)_lazyThisParameter == null)
            {
                Interlocked.CompareExchange(ref _lazyThisParameter, new ThisParameterSymbol(this), null);
            }

            thisParameter = _lazyThisParameter;
            return true;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        internal override bool IsNullableAnalysisEnabled() => false;

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal sealed override int? TryGetOverloadResolutionPriority()
        {
            return null;
        }
    }
}
