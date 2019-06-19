// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents the compiler generated value parameter for property/event accessor.
    /// This parameter has no source location/syntax, but may have attributes.
    /// Attributes with 'param' target specifier on the accessor must be applied to the this parameter.
    /// </summary>
    internal sealed class SynthesizedAccessorValueParameterSymbol : SourceComplexParameterSymbol
    {
        // Disallow/AllowNull attributes will be added to the `value` parameter if they were present on the property.
        private readonly FlowAnalysisAnnotations _annotations;

        public SynthesizedAccessorValueParameterSymbol(SourceMemberMethodSymbol accessor, TypeWithAnnotations paramType, int ordinal, FlowAnalysisAnnotations annotations)
            : base(accessor, ordinal, paramType, RefKind.None, ParameterSymbol.ValueParameterName, accessor.Locations,
                   syntaxRef: null,
                   defaultSyntaxValue: ConstantValue.Unset, // the default value can be set via [param: DefaultParameterValue] applied on the accessor
                   isParams: false,
                   isExtensionMethodThis: false)
        {
            Debug.Assert((annotations & ~(FlowAnalysisAnnotations.AllowNull | FlowAnalysisAnnotations.DisallowNull)) == 0);

            _annotations = annotations;
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
            => _annotations;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty; // since RefKind is always None.
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        protected override IAttributeTargetSymbol AttributeOwner
        {
            get { return (SourceMemberMethodSymbol)this.ContainingSymbol; }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // Bind the attributes on the accessor's attribute syntax list with "param" target specifier.
            var accessor = (SourceMemberMethodSymbol)this.ContainingSymbol;
            return accessor.GetAttributeDeclarations();
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            if ((_annotations & FlowAnalysisAnnotations.DisallowNull) != 0)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_CodeAnalysis_DisallowNullAttribute__ctor));
            }
            if ((_annotations & FlowAnalysisAnnotations.AllowNull) != 0)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_CodeAnalysis_AllowNullAttribute__ctor));
            }
        }
    }
}
