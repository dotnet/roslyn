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

        public sealed override bool IsImplicitlyDeclared => true;

        public sealed override bool IsDiscard => _originalParam.IsDiscard;

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                // Since you can't get from the syntax node that represents the original parameter 
                // back to this symbol we decided not to return the original syntax node here.
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public sealed override bool IsParams
        {
            get { return !_suppressOptional && _originalParam.IsParams; }
        }

        internal sealed override bool IsMetadataOptional
        {
            get
            {
                // pseudo-custom attributes are not suppressed:
                return _suppressOptional ? _originalParam.HasOptionalAttribute : _originalParam.IsMetadataOptional;
            }
        }

        internal sealed override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                // pseudo-custom attributes are not suppressed:
                return _suppressOptional ? _originalParam.DefaultValueFromAttributes : _originalParam.ExplicitDefaultConstantValue;
            }
        }

        internal sealed override ConstantValue DefaultValueFromAttributes
        {
            get { return _originalParam.DefaultValueFromAttributes; }
        }

        internal sealed override DeclarationScope Scope => _originalParam.Scope;

        public sealed override bool IsNullChecked => _originalParam.IsNullChecked;

        #region Forwarded

        public sealed override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _originalParam.TypeWithAnnotations; }
        }

        public sealed override RefKind RefKind
        {
            get { return _originalParam.RefKind; }
        }

        internal sealed override bool IsMetadataIn
        {
            get { return _originalParam.IsMetadataIn; }
        }

        internal sealed override bool IsMetadataOut
        {
            get { return _originalParam.IsMetadataOut; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return _originalParam.Locations; }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalParam.GetAttributes();
        }

        public sealed override string Name
        {
            get { return _originalParam.Name; }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _originalParam.RefCustomModifiers; }
        }

        internal sealed override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return _originalParam.MarshallingInformation; }
        }

        internal sealed override bool IsIDispatchConstant
        {
            get { return _originalParam.IsIDispatchConstant; }
        }

        internal sealed override bool IsIUnknownConstant
        {
            get { return _originalParam.IsIUnknownConstant; }
        }

        internal sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal sealed override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
        }

        internal sealed override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable;

        internal sealed override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable;

        #endregion
    }
}
