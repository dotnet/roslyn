// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated field.
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

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            // do not emit Dynamic or CompilerGenerated attributes for fields inside compiler generated types:
            if (_containingType.IsImplicitlyDeclared)
            {
                return;
            }

            CSharpCompilation compilation = this.DeclaringCompilation;

            // Assume that someone checked earlier that the attribute ctor is available and has no use-site errors.
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

            // TODO (tomat): do we need to emit dynamic attribute on any synthesized field?
            if (this.Type.TypeSymbol.ContainsDynamic())
            {
                // Assume that someone checked earlier that the attribute ctor is available and has no use-site errors.
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.Type.TypeSymbol, this.Type.CustomModifiers.Length));
            }

            if (this.Type.ContainsNullableReferenceTypes())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeNullableAttribute(this.Type));
            }
        }

        internal abstract override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

        public override string Name
        {
            get { return _name; }
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
