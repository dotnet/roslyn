// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly NamedTypeSymbol containingType;
        private readonly string name;
        private readonly DeclarationModifiers modifiers;

        public SynthesizedFieldSymbolBase(
            NamedTypeSymbol containingType,
            string name,
            bool isPublic,
            bool isReadOnly,
            bool isStatic)
        {
            Debug.Assert((object)containingType != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            this.containingType = containingType;
            this.name = name;
            this.modifiers = (isPublic ? DeclarationModifiers.Public : DeclarationModifiers.Private) |
                (isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None) |
                (isStatic ? DeclarationModifiers.Static : DeclarationModifiers.None);
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            // do not emit Dynamic or CompilerGenerated attributes for fields inside compiler generated types:
            if (containingType.IsImplicitlyDeclared)
            {
                return;
            }

            CSharpCompilation compilation = this.DeclaringCompilation;

            // Assume that someone checked earlier that the attribute ctor is available and has no use-site errors.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

            // TODO (tomat): do we need to emit dynamic attribute on any synthesized field?
            if (this.Type.ContainsDynamic())
            {
                // Assume that someone checked earlier that the attribute ctor is available and has no use-site errors.
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.Type, customModifiersCount: 0));
            }
        }

        /// <summary>
        /// Each hoisted iterator/async local has an associated index (1-based).
        /// </summary>
        internal abstract int IteratorLocalIndex { get; }

        internal abstract override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound);

        public override string Name
        {
            get { return this.name; }
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
            get { return (this.modifiers & DeclarationModifiers.ReadOnly) != 0; }
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
            get { return this.containingType; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
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
            get { return ModifierUtils.EffectiveAccessibility(this.modifiers); }
        }

        public override bool IsStatic
        {
            get { return (this.modifiers & DeclarationModifiers.Static) != 0; }
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