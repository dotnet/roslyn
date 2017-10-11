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
    internal sealed class SynthesizedBackingFieldSymbol : SynthesizedFieldSymbolBase, IAttributeTargetSymbol
    {
        private readonly SourcePropertySymbol _property;
        private readonly bool _hasInitializer;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;

        public SynthesizedBackingFieldSymbol(
            SourcePropertySymbol property,
            string name,
            bool isReadOnly,
            bool isStatic,
            bool hasInitializer)
            : base(property.ContainingType, name, isPublic: false, isReadOnly: isReadOnly, isStatic: isStatic)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            _property = property;
            _hasInitializer = hasInitializer;
        }

        public bool HasInitializer
        {
            get
            {
                return _hasInitializer;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _property;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _property.Locations;
            }
        }

        internal override bool SuppressDynamicAttribute
        {
            get
            {
                return false;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _property.Type;
        }

        internal override bool HasPointerType
        {
            get
            {
                return _property.HasPointerType;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
            => GetAttributesBag().Attributes;

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
            => this;

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
            => AttributeLocation.Field;

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
            => AttributeLocation.Field;

        internal override Location ErrorLocation
            => _property.Location;

        internal sealed override int? TypeLayoutOffset
            => GetDecodedWellKnownAttributeData()?.Offset;

        internal sealed override MarshalPseudoCustomAttributeData MarshallingInformation
            => GetDecodedWellKnownAttributeData()?.MarshallingInformation;

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;

            // Dev11 doesn't synthesize this attribute, the debugger has a knowledge
            // of special name C# compiler uses for backing fields, which is not desirable.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute());
        }

        internal sealed override bool IsNotSerialized
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasNonSerializedAttribute;
            }
        }

        internal sealed override bool HasSpecialName
        {
            get
            {
                if (HasRuntimeSpecialName)
                {
                    return true;
                }

                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                var containingType = (SourceMemberContainerTypeSymbol)_property.ContainingType;
                if (!containingType.AnyMemberHasAttributes)
                {
                    return null;
                }

                var lazyCustomAttributesBag = _lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    var data = (CommonFieldEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
                    return data?.ObsoleteAttributeData;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CommonFieldWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonFieldWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns a bag of custom attributes applied on the backing field and data decoded from well-known attributes. Returns null if there are no attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            var bag = _lazyCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            // We let the property drive and track completion
            _ = LoadAndValidateAttributes(OneOrMany.Create(_property.CSharpSyntaxNode.AttributeLists), ref _lazyCustomAttributesBag, AttributeLocation.Field);

            Debug.Assert(_lazyCustomAttributesBag.IsSealed);
            return _lazyCustomAttributesBag;
        }

        internal sealed override CSharpAttributeData EarlyDecodeWellKnownAttribute(
            ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            CSharpAttributeData boundAttribute;
            ObsoleteAttributeData obsoleteData;

            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<CommonFieldEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return boundAttribute;
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        internal sealed override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.Field);

            SourceFieldSymbol.DecodeWellKnownFieldAttribute(this, ref arguments, attribute);
        }

        internal override void PostDecodeWellKnownAttributes(
            ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes,
            DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.Field);

            SourceFieldSymbol.PostDecodeWellKnownFieldAttributes(this, boundAttributes, allAttributeSyntaxNodes, diagnostics, decodedData);

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }
    }
}
