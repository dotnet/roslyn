// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a parameter that is based on another parameter.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedParameterSymbol : ParameterSymbol
    {
        protected readonly ParameterSymbol _underlyingParameter;

        protected WrappedParameterSymbol(ParameterSymbol underlyingParameter)
        {
            Debug.Assert((object)underlyingParameter != null);

            this._underlyingParameter = underlyingParameter;
        }

        public ParameterSymbol UnderlyingParameter => _underlyingParameter;

        public sealed override bool IsDiscard => _underlyingParameter.IsDiscard;

        #region Forwarded

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _underlyingParameter.TypeWithAnnotations; }
        }

        public sealed override RefKind RefKind
        {
            get { return _underlyingParameter.RefKind; }
        }

        internal sealed override bool IsMetadataIn
        {
            get { return _underlyingParameter.IsMetadataIn; }
        }

        internal sealed override bool IsMetadataOut
        {
            get { return _underlyingParameter.IsMetadataOut; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return _underlyingParameter.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return _underlyingParameter.DeclaringSyntaxReferences; }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingParameter.GetAttributes();
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            _underlyingParameter.AddSynthesizedAttributes(moduleBuilder, ref attributes);
        }

        internal sealed override ConstantValue? ExplicitDefaultConstantValue
        {
            get { return _underlyingParameter.ExplicitDefaultConstantValue; }
        }

        internal sealed override ConstantValue? DefaultValueFromAttributes
        {
            get { return _underlyingParameter.DefaultValueFromAttributes; }
        }

        public override int Ordinal
        {
            get { return _underlyingParameter.Ordinal; }
        }

        public override bool IsParamsArray
        {
            get { return _underlyingParameter.IsParamsArray; }
        }

        public override bool IsParamsCollection
        {
            get { return _underlyingParameter.IsParamsCollection; }
        }

        internal override bool IsMetadataOptional
        {
            get { return _underlyingParameter.IsMetadataOptional; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingParameter.IsImplicitlyDeclared; }
        }

        public sealed override string Name
        {
            get { return _underlyingParameter.Name; }
        }

        public sealed override string MetadataName
        {
            get { return _underlyingParameter.MetadataName; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _underlyingParameter.RefCustomModifiers; }
        }

        internal override MarshalPseudoCustomAttributeData? MarshallingInformation
        {
            get { return _underlyingParameter.MarshallingInformation; }
        }

        internal override UnmanagedType MarshallingType
        {
            get { return _underlyingParameter.MarshallingType; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return _underlyingParameter.IsIDispatchConstant; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return _underlyingParameter.IsIUnknownConstant; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            // https://github.com/dotnet/roslyn/issues/30073: Consider moving to leaf types
            get { return _underlyingParameter.FlowAnalysisAnnotations; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return _underlyingParameter.NotNullIfParameterNotNull; }
        }

        public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
        {
            return _underlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal sealed override ScopedKind DeclaredScope => _underlyingParameter.DeclaredScope;

        internal sealed override ScopedKind EffectiveScope => _underlyingParameter.EffectiveScope;

        internal sealed override bool HasUnscopedRefAttribute => _underlyingParameter.HasUnscopedRefAttribute;

        internal sealed override bool UseUpdatedEscapeRules => _underlyingParameter.UseUpdatedEscapeRules;

        #endregion
    }
}
