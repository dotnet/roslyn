// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a source parameter cloned from another <see cref="SourceParameterSymbol"/>, when they must share attribute data and default constant value.
    /// For example, parameters on a property symbol are cloned to generate parameters on accessors.
    /// Similarly parameters on delegate invoke method are cloned to delegate begin/end invoke methods.
    /// </summary>
    internal abstract class SourceClonedParameterSymbol : SourceParameterSymbolBase
    {
        // if true suppresses params-array and default value:
        private readonly bool _suppressOptional;

        protected readonly SourceParameterSymbol _originalParam;

        internal SourceClonedParameterSymbol(SourceParameterSymbol originalParam, Symbol newOwner, int newOrdinal, bool suppressOptional)
            : base(newOwner, newOrdinal)
        {
            Debug.Assert((object)originalParam != null);
            _suppressOptional = suppressOptional;
            _originalParam = originalParam;
        }

        public override bool IsImplicitlyDeclared => true;

        public override bool IsDiscard => _originalParam.IsDiscard;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                // Since you can't get from the syntax node that represents the original parameter 
                // back to this symbol we decided not to return the original syntax node here.
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool IsParamsArray
        {
            get { return !_suppressOptional && _originalParam.IsParamsArray; }
        }

        public override bool IsParamsCollection
        {
            get { return !_suppressOptional && _originalParam.IsParamsCollection; }
        }

        internal override bool IsMetadataOptional
        {
            get
            {
                // pseudo-custom attributes are not suppressed:
                return _suppressOptional ? _originalParam.HasOptionalAttribute : _originalParam.IsMetadataOptional;
            }
        }

        internal sealed override ScopedKind EffectiveScope => _originalParam.EffectiveScope;

        internal override bool HasUnscopedRefAttribute => _originalParam.HasUnscopedRefAttribute;

        internal sealed override bool UseUpdatedEscapeRules => _originalParam.UseUpdatedEscapeRules;

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                // pseudo-custom attributes are not suppressed:
                return _suppressOptional ? _originalParam.DefaultValueFromAttributes : _originalParam.ExplicitDefaultConstantValue;
            }
        }

        internal override ConstantValue DefaultValueFromAttributes
        {
            get { return _originalParam.DefaultValueFromAttributes; }
        }

        #region Forwarded

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _originalParam.TypeWithAnnotations; }
        }

        public override RefKind RefKind
        {
            get { return _originalParam.RefKind; }
        }

        internal override bool IsMetadataIn
        {
            get { return _originalParam.IsMetadataIn; }
        }

        internal override bool IsMetadataOut
        {
            get { return _originalParam.IsMetadataOut; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _originalParam.Locations; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalParam.GetAttributes();
        }

        public sealed override string Name
        {
            get { return _originalParam.Name; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _originalParam.RefCustomModifiers; }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return _originalParam.MarshallingInformation; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return _originalParam.IsIDispatchConstant; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return _originalParam.IsIUnknownConstant; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
        }

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable();

        internal override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable();

        #endregion
    }
}
