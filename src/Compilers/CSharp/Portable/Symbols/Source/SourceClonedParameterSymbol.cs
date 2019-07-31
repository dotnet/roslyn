// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a source parameter cloned from another <see cref="SourceParameterSymbol"/>, when they must share attribute data and default constant value.
    /// For example, parameters on a property symbol are cloned to generate parameters on accessors.
    /// Similarly parameters on delegate invoke method are cloned to delegate begin/end invoke methods.
    /// </summary>
    internal sealed class SourceClonedParameterSymbol : SourceParameterSymbolBase
    {
        // if true suppresses params-array and default value:
        private readonly bool _suppressOptional;

        private readonly SourceParameterSymbol _originalParam;

        internal SourceClonedParameterSymbol(SourceParameterSymbol originalParam, Symbol newOwner, int newOrdinal, bool suppressOptional)
            : base(newOwner, newOrdinal)
        {
            Debug.Assert((object)originalParam != null);

            _suppressOptional = suppressOptional;
            _originalParam = originalParam;
        }

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                // Since you can't get from the syntax node that represents the original parameter 
                // back to this symbol we decided not to return the original syntax node here.
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool IsParams
        {
            get { return !_suppressOptional && _originalParam.IsParams; }
        }

        internal override bool IsMetadataOptional
        {
            get
            {
                // pseudo-custom attributes are not suppressed:
                return _suppressOptional ? _originalParam.HasOptionalAttribute : _originalParam.IsMetadataOptional;
            }
        }

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

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            return new SourceClonedParameterSymbol(
                _originalParam.WithCustomModifiersAndParamsCore(newType, newCustomModifiers, newRefCustomModifiers, newIsParams),
                this.ContainingSymbol,
                this.Ordinal,
                _suppressOptional);
        }

        public override bool IsNullChecked => _originalParam.IsNullChecked;

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

        internal override bool IsCallerFilePath
        {
            get { return _originalParam.IsCallerFilePath; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return _originalParam.IsCallerLineNumber; }
        }

        internal override bool IsCallerMemberName
        {
            get { return _originalParam.IsCallerMemberName; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
        }

        #endregion
    }
}
