// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a simple compiler generated parameter of a given type.
    /// </summary>
    internal abstract class SynthesizedParameterSymbolBase : ParameterSymbol
    {
        private readonly MethodSymbol _container;
        private readonly TypeWithAnnotations _type;
        private readonly int _ordinal;
        private readonly string _name;
        private readonly RefKind _refKind;

        public SynthesizedParameterSymbolBase(
            MethodSymbol container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            string name = "")
        {
            Debug.Assert(type.HasType);
            Debug.Assert(name != null);
            Debug.Assert(ordinal >= 0);

            _container = container;
            _type = type;
            _ordinal = ordinal;
            _refKind = refKind;
            _name = name;
        }

        public override TypeWithAnnotations TypeWithAnnotations => _type;

        public override RefKind RefKind => _refKind;

        public sealed override bool IsDiscard => false;

        internal override bool IsMetadataIn => RefKind == RefKind.In;

        internal override bool IsMetadataOut => RefKind == RefKind.Out;

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return null; }
        }

        public override string Name
        {
            get { return _name; }
        }

        public abstract override ImmutableArray<CustomModifier> RefCustomModifiers { get; }

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

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
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

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            // Emit [Dynamic] on synthesized parameter symbols when the original parameter was dynamic 
            // in order to facilitate debugging.  In the case the necessary attributes are missing 
            // this is a no-op.  Emitting an error here, or when the original parameter was bound, would
            // adversely effect the compilation or potentially change overload resolution.  
            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;
            if (type.Type.ContainsDynamic() && compilation.HasDynamicEmitAttributes() && compilation.CanEmitBoolean())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length + this.RefCustomModifiers.Length, this.RefKind));
            }

            if (type.Type.ContainsTupleNames() &&
                compilation.HasTupleNamesAttributes &&
                compilation.CanEmitSpecialType(SpecialType.System_String))
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, GetNullableContextValue(), type));
            }

            if (this.RefKind == RefKind.RefReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }
        }
    }

    internal sealed class SynthesizedParameterSymbol : SynthesizedParameterSymbolBase
    {
        private SynthesizedParameterSymbol(
            MethodSymbol container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            string name)
            : base(container, type, ordinal, refKind, name)
        {
        }

        public static ParameterSymbol Create(
            MethodSymbol container,
            TypeWithAnnotations type,
            int ordinal,
            RefKind refKind,
            string name = "",
            ImmutableArray<CustomModifier> refCustomModifiers = default,
            ImmutableArray<CSharpAttributeData> attributes = default)
        {
            if (refCustomModifiers.IsDefaultOrEmpty && attributes.IsDefaultOrEmpty)
            {
                return new SynthesizedParameterSymbol(container, type, ordinal, refKind, name);
            }

            return new SynthesizedComplexParameterSymbol(container, type, ordinal, refKind, name, refCustomModifiers.NullToEmpty(), attributes.NullToEmpty());
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
                builder.Add(SynthesizedParameterSymbol.Create(destinationMethod, oldParam.TypeWithAnnotations, oldParam.Ordinal,
                    oldParam.RefKind, oldParam.Name, oldParam.RefCustomModifiers));
            }

            return builder.ToImmutableAndFree();
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        private sealed class SynthesizedComplexParameterSymbol : SynthesizedParameterSymbolBase
        {
            private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
            private readonly ImmutableArray<CSharpAttributeData> _attributes;

            public SynthesizedComplexParameterSymbol(
                MethodSymbol container,
                TypeWithAnnotations type,
                int ordinal,
                RefKind refKind,
                string name,
                ImmutableArray<CustomModifier> refCustomModifiers,
                ImmutableArray<CSharpAttributeData> attributes)
                : base(container, type, ordinal, refKind, name)
            {
                Debug.Assert(!refCustomModifiers.IsDefault);
                Debug.Assert(!attributes.IsDefault);

                _refCustomModifiers = refCustomModifiers;
                _attributes = attributes;
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get { return _refCustomModifiers; }
            }

            public override ImmutableArray<CSharpAttributeData> GetAttributes()
            {
                return _attributes;
            }
        }
    }
}
