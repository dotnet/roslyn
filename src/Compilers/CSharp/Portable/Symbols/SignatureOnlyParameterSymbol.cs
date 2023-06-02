// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Intended to be used to create ParameterSymbols for a SignatureOnlyMethodSymbol.
    /// </summary>
    internal sealed class SignatureOnlyParameterSymbol : ParameterSymbol
    {
        private readonly TypeWithAnnotations _type;
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        private readonly bool _isParams;
        private readonly RefKind _refKind;

        public SignatureOnlyParameterSymbol(
            TypeWithAnnotations type,
            ImmutableArray<CustomModifier> refCustomModifiers,
            bool isParams,
            RefKind refKind)
        {
            Debug.Assert((object)type.Type != null);
            Debug.Assert(!refCustomModifiers.IsDefault);

            _type = type;
            _refCustomModifiers = refCustomModifiers;
            _isParams = isParams;
            _refKind = refKind;
        }

        public override TypeWithAnnotations TypeWithAnnotations { get { return _type; } }

        public override ImmutableArray<CustomModifier> RefCustomModifiers { get { return _refCustomModifiers; } }

        public override bool IsParams { get { return _isParams; } }

        public override RefKind RefKind { get { return _refKind; } }

        public override string Name { get { return ""; } }

        public override bool IsImplicitlyDeclared { get { return true; } }

        public override bool IsDiscard { get { return false; } }

        internal override ScopedKind EffectiveScope
            => ParameterHelpers.IsRefScopedByDefault(this) ? ScopedKind.ScopedRef : ScopedKind.None;

        internal override bool HasUnscopedRefAttribute => false;

        internal override bool UseUpdatedEscapeRules => false;

        #region Not used by MethodSignatureComparer

        internal override bool IsMetadataIn { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsMetadataOut { get { throw ExceptionUtilities.Unreachable(); } }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation { get { throw ExceptionUtilities.Unreachable(); } }

        public override int Ordinal { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsMetadataOptional { get { throw ExceptionUtilities.Unreachable(); } }

        internal override ConstantValue ExplicitDefaultConstantValue { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsIDispatchConstant { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsIUnknownConstant { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsCallerFilePath { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsCallerLineNumber { get { throw ExceptionUtilities.Unreachable(); } }

        internal override bool IsCallerMemberName { get { throw ExceptionUtilities.Unreachable(); } }

        internal override int CallerArgumentExpressionParameterIndex { get { throw ExceptionUtilities.Unreachable(); } }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations { get { throw ExceptionUtilities.Unreachable(); } }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull { get { throw ExceptionUtilities.Unreachable(); } }

        public override Symbol ContainingSymbol { get { throw ExceptionUtilities.Unreachable(); } }

        public override ImmutableArray<Location> Locations { get { throw ExceptionUtilities.Unreachable(); } }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get { throw ExceptionUtilities.Unreachable(); } }

        public override AssemblySymbol ContainingAssembly { get { throw ExceptionUtilities.Unreachable(); } }

        internal override ModuleSymbol ContainingModule { get { throw ExceptionUtilities.Unreachable(); } }

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable();

        internal override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable();

        #endregion Not used by MethodSignatureComparer

        public override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if ((object)this == obj)
            {
                return true;
            }

            var other = obj as SignatureOnlyParameterSymbol;
            return other is not null &&
                TypeSymbol.Equals(_type.Type, other._type.Type, compareKind) &&
                _type.CustomModifiers.Equals(other._type.CustomModifiers) &&
                _refCustomModifiers.SequenceEqual(other._refCustomModifiers) &&
                _isParams == other._isParams &&
                _refKind == other._refKind;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                _type.Type.GetHashCode(),
                Hash.Combine(
                    Hash.CombineValues(_type.CustomModifiers),
                    Hash.Combine(
                        _isParams.GetHashCode(),
                        ((int)_refKind).GetHashCode())));
        }
    }
}
