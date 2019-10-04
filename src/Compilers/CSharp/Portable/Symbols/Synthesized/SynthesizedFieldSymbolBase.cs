// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated field or captured variable.
    /// </summary>
    internal abstract class SynthesizedFieldSymbolBase : FieldSymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private readonly string _name;
        private readonly DeclarationModifiers _modifiers;

        public SynthesizedFieldSymbolBase(
            NamedTypeSymbol containingType,
            string name,
            bool isPublic,
            bool isReadOnly,
            bool isStatic)
        {
            RoslynDebug.Assert((object)containingType != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            _containingType = containingType;
            _name = name;
            _modifiers = (isPublic ? DeclarationModifiers.Public : DeclarationModifiers.Private) |
                (isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None) |
                (isStatic ? DeclarationModifiers.Static : DeclarationModifiers.None);
        }

        internal abstract bool SuppressDynamicAttribute
        {
            get;
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData>? attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            CSharpCompilation compilation = this.DeclaringCompilation;
            var typeWithAnnotations = this.TypeWithAnnotations;
            var type = typeWithAnnotations.Type;

            // do not emit CompilerGenerated attributes for fields inside compiler generated types:
            if (!_containingType.IsImplicitlyDeclared)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            if (!this.SuppressDynamicAttribute &&
                type.ContainsDynamic() &&
                compilation.HasDynamicEmitAttributes() &&
                compilation.CanEmitBoolean())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type, typeWithAnnotations.CustomModifiers.Length));
            }

            if (type.ContainsTupleNames() &&
                compilation.HasTupleNamesAttributes &&
                compilation.CanEmitSpecialType(SpecialType.System_String))
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, ContainingType.GetNullableContextValue(), typeWithAnnotations));
            }
        }

        internal abstract override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations
            => FlowAnalysisAnnotations.None;

        public override string Name
        {
            get { return _name; }
        }

        public override Symbol? AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public override bool IsReadOnly
        {
            get { return (_modifiers & DeclarationModifiers.ReadOnly) != 0; }
        }

        public override bool IsVolatile
        {
            get { return false; }
        }

        public override bool IsConst
        {
            get { return false; }
        }

        internal override bool IsNotSerialized
        {
            get { return false; }
        }

        internal override MarshalPseudoCustomAttributeData? MarshallingInformation
        {
            get { return null; }
        }

        internal override int? TypeLayoutOffset
        {
            get { return null; }
        }

        internal override ConstantValue? GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return null;
        }

        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
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

        public override Accessibility DeclaredAccessibility
        {
            get { return ModifierUtils.EffectiveAccessibility(_modifiers); }
        }

        public override bool IsStatic
        {
            get { return (_modifiers & DeclarationModifiers.Static) != 0; }
        }

        internal override bool HasSpecialName
        {
            get { return this.HasRuntimeSpecialName; }
        }

        internal override bool HasRuntimeSpecialName
        {
            get { return this.Name == WellKnownMemberNames.EnumBackingFieldName; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}
