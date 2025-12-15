// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A base class for synthesized methods that might want a this parameter.
    /// </summary>
    internal abstract class SynthesizedMethodSymbol : MethodSymbol
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

        public abstract override bool IsStatic { get; }

#nullable enable

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            Debug.Assert(!this.IsExtensionBlockMember());

            if (IsStatic)
            {
                thisParameter = null;
                return true;
            }

            if ((object)_lazyThisParameter == null)
            {
                Interlocked.CompareExchange(ref _lazyThisParameter, new ThisParameterSymbol(this), null);
            }

            thisParameter = _lazyThisParameter;
            return true;
        }

#nullable disable

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        internal override bool IsNullableAnalysisEnabled() => false;

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => ContainingModule.UseUpdatedEscapeRules;

        internal sealed override bool IsCallerUnsafe => false;

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal sealed override int TryGetOverloadResolutionPriority()
        {
            return 0;
        }
    }
}
