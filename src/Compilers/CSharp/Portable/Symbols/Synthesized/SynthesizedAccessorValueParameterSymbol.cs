// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
    internal sealed class SynthesizedAccessorValueParameterSymbol : SourceComplexParameterSymbolBase
    {
        public SynthesizedAccessorValueParameterSymbol(SourceMemberMethodSymbol accessor, TypeWithAnnotations paramType, int ordinal)
            : base(accessor, ordinal, paramType, RefKind.None, ParameterSymbol.ValueParameterName, accessor.TryGetFirstLocation(),
                   syntaxRef: null,
                   isParams: false,
                   isExtensionMethodThis: false,
                   scope: ScopedKind.None)
        {
            Debug.Assert(accessor.Locations.Length <= 1);
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                var result = FlowAnalysisAnnotations.None;
                if (ContainingSymbol is SourcePropertyAccessorSymbol propertyAccessor && propertyAccessor.AssociatedSymbol is SourcePropertySymbolBase property)
                {
                    if (property.HasDisallowNull)
                    {
                        result |= FlowAnalysisAnnotations.DisallowNull;
                    }
                    if (property.HasAllowNull)
                    {
                        result |= FlowAnalysisAnnotations.AllowNull;
                    }
                }
                return result;
            }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

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

            if (ContainingSymbol is SourcePropertyAccessorSymbol propertyAccessor && propertyAccessor.AssociatedSymbol is SourcePropertySymbolBase property)
            {
                var annotations = FlowAnalysisAnnotations;
                if ((annotations & FlowAnalysisAnnotations.DisallowNull) != 0)
                {
                    AddSynthesizedAttribute(ref attributes, SynthesizedAttributeData.Create(property.DisallowNullAttributeIfExists));
                }
                if ((annotations & FlowAnalysisAnnotations.AllowNull) != 0)
                {
                    AddSynthesizedAttribute(ref attributes, SynthesizedAttributeData.Create(property.AllowNullAttributeIfExists));
                }
            }
        }
    }
}
