// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
        public SynthesizedAccessorValueParameterSymbol(SourceMethodSymbol accessor, TypeSymbolWithAnnotations paramType, int ordinal)
            : base(accessor, ordinal, paramType, RefKind.None, ParameterSymbol.ValueParameterName, accessor.Locations,
                   syntaxRef: null,
                   defaultSyntaxValue: ConstantValue.Unset, // the default value can be set via [param: DefaultParameterValue] applied on the accessor
                   isParams: false,
                   isExtensionMethodThis: false)
        {
        }

        internal override ushort CountOfCustomModifiersPrecedingByRef
        {
            get
            {
                return 0; // since RefKind is always None.
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }


        protected override IAttributeTargetSymbol AttributeOwner
        {
            get { return (SourceMethodSymbol)this.ContainingSymbol; }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // Bind the attributes on the accessor's attribute syntax list with "param" target specifier.
            var accessor = (SourceMethodSymbol)this.ContainingSymbol;
            return accessor.GetAttributeDeclarations();
        }
    }
}
