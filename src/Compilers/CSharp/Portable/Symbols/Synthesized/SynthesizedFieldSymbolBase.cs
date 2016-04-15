// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
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
            Debug.Assert((object)containingType != null);
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

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            CSharpCompilation compilation = this.DeclaringCompilation;

            // do not emit CompilerGenerated attributes for fields inside compiler generated types:
            if (!_containingType.IsImplicitlyDeclared)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            if (!this.SuppressDynamicAttribute &&
                this.Type.ContainsDynamic() &&
                compilation.HasDynamicEmitAttributes() &&
                compilation.CanEmitBoolean())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.Type, this.CustomModifiers.Length));
            }
        }

        internal abstract override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

        public override string Name
        {
            get { return _name; }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override Symbol AssociatedSymbol
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

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get { return null; }
        }

        internal override int? TypeLayoutOffset
        {
            get { return null; }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            return null;
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
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
