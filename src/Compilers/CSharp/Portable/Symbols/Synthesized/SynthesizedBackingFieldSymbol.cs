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
    /// Represents a compiler generated backing field for an automatically implemented property.
    /// </summary>
    internal sealed class SynthesizedBackingFieldSymbol : FieldSymbolWithAttributesAndModifiers
    {
        private readonly SourcePropertySymbol _property;
        private readonly string _name;
        internal bool HasInitializer { get; }
        protected override DeclarationModifiers Modifiers { get; }

        public SynthesizedBackingFieldSymbol(
            SourcePropertySymbol property,
            string name,
            bool isReadOnly,
            bool isStatic,
            bool hasInitializer)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            _name = name;

            Modifiers = DeclarationModifiers.Private |
                (isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None) |
                (isStatic ? DeclarationModifiers.Static : DeclarationModifiers.None);

            _property = property;
            HasInitializer = hasInitializer;
        }

        protected override IAttributeTargetSymbol AttributeOwner
            => _property;

        internal override Location ErrorLocation
            => _property.Location;

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => _property.CSharpSyntaxNode.AttributeLists;

        public override Symbol AssociatedSymbol
            => _property;

        public override ImmutableArray<Location> Locations
            => _property.Locations;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            => _property.TypeWithAnnotations;

        internal override bool HasPointerType
            => _property.HasPointerType;

        internal sealed override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.FixedBufferAttribute))
            {
                // error CS8362: Do not use 'System.Runtime.CompilerServices.FixedBuffer' attribute on property
                arguments.Diagnostics.Add(ErrorCode.ERR_DoNotUseFixedBufferAttrOnProperty, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else
            {
                base.DecodeWellKnownAttribute(ref arguments);
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;

            // do not emit CompilerGenerated attributes for fields inside compiler generated types:
            if (!this.ContainingType.IsImplicitlyDeclared)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            // Dev11 doesn't synthesize this attribute, the debugger has a knowledge
            // of special name C# compiler uses for backing fields, which is not desirable.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute());
        }

        public override string Name
            => _name;

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
            => null;

        public override Symbol ContainingSymbol
            => _property.ContainingSymbol;

        public override NamedTypeSymbol ContainingType
            => _property.ContainingType;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray<SyntaxReference>.Empty;

        internal override bool HasRuntimeSpecialName
            => false;

        public override bool IsImplicitlyDeclared
            => true;
    }
}
