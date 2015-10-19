﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a simple compiler generated parameter of a given type.
    /// </summary>
    internal class SynthesizedParameterSymbol : ParameterSymbol
    {
        private readonly MethodSymbol _container;
        private readonly TypeSymbol _type;
        private readonly int _ordinal;
        private readonly string _name;
        private readonly ImmutableArray<CustomModifier> _customModifiers;
        private readonly RefKind _refKind;

        public SynthesizedParameterSymbol(
            MethodSymbol container,
            TypeSymbol type,
            int ordinal,
            RefKind refKind,
            string name = "",
            ImmutableArray<CustomModifier> customModifiers = default(ImmutableArray<CustomModifier>))
        {
            Debug.Assert((object)type != null);
            Debug.Assert(name != null);
            Debug.Assert(ordinal >= 0);

            _container = container;
            _type = type;
            _ordinal = ordinal;
            _refKind = refKind;
            _name = name;
            _customModifiers = customModifiers.NullToEmpty();
        }

        public override TypeSymbol Type
        {
            get { return _type; }
        }

        public override RefKind RefKind
        {
            get { return _refKind; }
        }

        internal override bool IsMetadataIn
        {
            get { return false; }
        }

        internal override bool IsMetadataOut
        {
            get { return _refKind == RefKind.Out; }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return null; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get { return _customModifiers; }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override bool IsParams
        {
            get { return false; }
        }

        internal override bool IsMetadataOptional
        {
            get { return false; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get { return null; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return false; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return false; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return false; }
        }

        internal override bool IsCallerFilePath
        {
            get { return false; }
        }

        internal override bool IsCallerMemberName
        {
            get { return false; }
        }

        internal sealed override ushort CountOfCustomModifiersPrecedingByRef
        {
            get { return 0; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            // Emit [Dynamic] on synthesized parameter symbols when the original parameter was dynamic 
            // in order to facilitate debugging.  In the case the necessary attributes are missing 
            // this is a no-op.  Emitting an error here, or when the original parameter was bound, would
            // adversely effect the compilation or potentially change overload resolution.  
            var compilation = this.DeclaringCompilation;
            if (Type.ContainsDynamic() && 
                compilation.HasDynamicEmitAttributes() &&
                compilation.GetSpecialType(SpecialType.System_Boolean).SpecialType == SpecialType.System_Boolean)
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.Type, this.CustomModifiers.Length, this.RefKind));
            }
        }

        /// <summary>
        /// For each parameter of a source method, construct a corresponding synthesized parameter
        /// for a destination method.
        /// </summary>
        /// <param name="sourceMethod">Has parameters.</param>
        /// <param name="destinationMethod">Needs parameters.</param>
        /// <returns>Synthesized parameters to add to destination method.</returns>
        internal static ImmutableArray<ParameterSymbol> DeriveParameters(MethodSymbol sourceMethod, MethodSymbol destinationMethod)
        {
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();

            foreach (var oldParam in sourceMethod.Parameters)
            {
                //same properties as the old one, just change the owner
                builder.Add(new SynthesizedParameterSymbol(destinationMethod, oldParam.Type, oldParam.Ordinal,
                    oldParam.RefKind, oldParam.Name, oldParam.CustomModifiers));
            }

            return builder.ToImmutableAndFree();
        }
    }
}
