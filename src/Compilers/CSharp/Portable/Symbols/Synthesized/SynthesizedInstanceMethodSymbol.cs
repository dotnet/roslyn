﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A base class for synthesized methods that want a this parameter.
    /// </summary>
    internal abstract class SynthesizedInstanceMethodSymbol : MethodSymbol
    {
        private ParameterSymbol _lazyThisParameter;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        public sealed override bool AreLocalsZeroed
        {
            get
            {
                return ContainingType.AreLocalsZeroed;
            }
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            Debug.Assert(!IsStatic);

            if ((object)_lazyThisParameter == null)
            {
                Interlocked.CompareExchange(ref _lazyThisParameter, new ThisParameterSymbol(this), null);
            }

            thisParameter = _lazyThisParameter;
            return true;
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;
    }
}
