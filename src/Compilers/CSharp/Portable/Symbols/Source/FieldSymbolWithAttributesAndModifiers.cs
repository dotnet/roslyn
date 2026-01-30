// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class FieldSymbolWithAttributesAndModifiers : FieldSymbol, IAttributeTargetSymbol
    {
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;
        protected SymbolCompletionState state;
        internal abstract Location ErrorLocation { get; }
        protected abstract DeclarationModifiers Modifiers { get; }

        /// <summary>
        /// Gets the syntax list of custom attributes applied on the symbol.
        /// </summary>
        protected abstract OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations();

        protected abstract IAttributeTargetSymbol AttributeOwner { get; }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
            => this.AttributeOwner;

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
            => AttributeLocation.Field;

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
            => AttributeLocation.Field;

        internal sealed override bool HasComplete(CompletionPart part)
            => state.HasComplete(part);

        public sealed override bool IsStatic
            => (Modifiers & DeclarationModifiers.Static) != 0;

        public sealed override bool IsReadOnly
            => (Modifiers & DeclarationModifiers.ReadOnly) != 0;

        public sealed override Accessibility DeclaredAccessibility
            => ModifierUtils.EffectiveAccessibility(Modifiers);

        public sealed override bool IsConst
            => (Modifiers & DeclarationModifiers.Const) != 0;

        public sealed override bool IsVolatile
            => (Modifiers & DeclarationModifiers.Volatile) != 0;

        public sealed override bool IsFixedSizeBuffer
            => (Modifiers & DeclarationModifiers.Fixed) != 0;

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        /// <remarks>
        /// NOTE: This method should always be kept as a sealed override.
        /// If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        /// </remarks>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
            => this.GetAttributesBag().Attributes;

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes.
        /// Returns an empty bag if there are no attributes applied on the symbol.
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

            if (LoadAndValidateAttributes(this.GetAttributeDeclarations(), ref _lazyCustomAttributesBag))
            {
                var completed = state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(completed);
            }

            Debug.Assert(_lazyCustomAttributesBag.IsSealed);
            return _lazyCustomAttributesBag;
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        protected FieldWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (FieldWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

#nullable enable
        internal sealed override (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            CSharpAttributeData? attributeData;
            BoundAttribute? boundAttribute;
            ObsoleteAttributeData? obsoleteData;

            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out attributeData, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<CommonFieldEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return (attributeData, boundAttribute);
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }
#nullable disable

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                var containingSourceType = (SourceMemberContainerTypeSymbol)ContainingType;
                if (!containingSourceType.AnyMemberHasAttributes)
                {
                    return null;
                }

                var lazyCustomAttributesBag = _lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    var data = (CommonFieldEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
                    return data != null ? data.ObsoleteAttributeData : null;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

        protected override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<FieldWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.NonSerializedAttribute))
            {
                arguments.GetOrCreateData<FieldWellKnownAttributeData>().HasNonSerializedAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.FieldOffsetAttribute))
            {
                if (this.IsStatic || this.IsConst)
                {
                    // CS0637: The FieldOffset attribute is not allowed on static or const fields
                    diagnostics.Add(ErrorCode.ERR_StructOffsetOnBadField, arguments.AttributeSyntaxOpt.Name.Location);
                }
                else
                {
                    int offset = attribute.CommonConstructorArguments[0].DecodeValue<int>(SpecialType.System_Int32);
                    if (offset < 0)
                    {
                        // Dev10 reports CS0647: "Error emitting attribute ..."
                        diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attribute.GetAttributeArgumentLocation(0), arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                        offset = 0;
                    }

                    // Set field offset even if the attribute specifies an invalid value, so that
                    // post-validation knows that the attribute is applied and reports better errors.
                    arguments.GetOrCreateData<FieldWellKnownAttributeData>().SetFieldOffset(offset);
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.MarshalAsAttribute))
            {
                MarshalAsAttributeDecoder<FieldWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.Field, MessageProvider.Instance);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                permitted: ReservedAttributes.NullableContextAttribute
                    | ReservedAttributes.NullablePublicOnlyAttribute
                    | ReservedAttributes.CaseSensitiveExtensionAttribute
                    | ReservedAttributes.ScopedRefAttribute
                    | ReservedAttributes.RefSafetyRulesAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DateTimeConstantAttribute))
            {
                VerifyConstantValueMatches(attribute.DecodeDateTimeConstantValue(), ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DecimalConstantAttribute))
            {
                VerifyConstantValueMatches(attribute.DecodeDecimalConstantValue(), ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AllowNullAttribute))
            {
                arguments.GetOrCreateData<FieldWellKnownAttributeData>().HasAllowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DisallowNullAttribute))
            {
                arguments.GetOrCreateData<FieldWellKnownAttributeData>().HasDisallowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.MaybeNullAttribute))
            {
                arguments.GetOrCreateData<FieldWellKnownAttributeData>().HasMaybeNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.NotNullAttribute))
            {
                arguments.GetOrCreateData<FieldWellKnownAttributeData>().HasNotNullAttribute = true;
            }
        }

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations
            => DecodeFlowAnalysisAttributes(GetDecodedWellKnownAttributeData());

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(FieldWellKnownAttributeData attributeData)
        {
            var annotations = FlowAnalysisAnnotations.None;
            if (attributeData != null)
            {
                if (attributeData.HasAllowNullAttribute) annotations |= FlowAnalysisAnnotations.AllowNull;
                if (attributeData.HasDisallowNullAttribute) annotations |= FlowAnalysisAnnotations.DisallowNull;
                if (attributeData.HasMaybeNullAttribute) annotations |= FlowAnalysisAnnotations.MaybeNull;
                if (attributeData.HasNotNullAttribute) annotations |= FlowAnalysisAnnotations.NotNull;
            }
            return annotations;
        }

        /// <summary>
        /// Verify the constant value matches the default value from any earlier attribute
        /// (DateTimeConstantAttribute or DecimalConstantAttribute).
        /// If not, report ERR_FieldHasMultipleDistinctConstantValues.
        /// </summary>
        private void VerifyConstantValueMatches(ConstantValue attrValue, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            if (!attrValue.IsBad)
            {
                var data = arguments.GetOrCreateData<FieldWellKnownAttributeData>();
                ConstantValue constValue;
                var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

                if (this.IsConst)
                {
                    if (this.Type.SpecialType == SpecialType.System_Decimal)
                    {
                        constValue = this.GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false);

                        if ((object)constValue != null && !constValue.IsBad && constValue != attrValue)
                        {
                            diagnostics.Add(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.Location);
                        }
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.Location);
                    }

                    if (data.ConstValue == CodeAnalysis.ConstantValue.Unset)
                    {
                        data.ConstValue = attrValue;
                    }
                }
                else
                {
                    constValue = data.ConstValue;

                    if (constValue != CodeAnalysis.ConstantValue.Unset)
                    {
                        if (constValue != attrValue)
                        {
                            diagnostics.Add(ErrorCode.ERR_FieldHasMultipleDistinctConstantValues, arguments.AttributeSyntaxOpt.Location);
                        }
                    }
                    else
                    {
                        data.ConstValue = attrValue;
                    }
                }
            }
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.None);

            var data = (FieldWellKnownAttributeData)decodedData;
            int? fieldOffset = data != null ? data.Offset : null;

            if (fieldOffset.HasValue)
            {
                if (this.ContainingType.Layout.Kind != LayoutKind.Explicit)
                {
                    Debug.Assert(boundAttributes.Any());

                    // error CS0636: The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)
                    int i = boundAttributes.IndexOfAttribute(AttributeDescription.FieldOffsetAttribute);
                    diagnostics.Add(ErrorCode.ERR_StructOffsetOnBadStruct, allAttributeSyntaxNodes[i].Name.Location);
                }
            }
            else if (!this.IsStatic && !this.IsConst)
            {
                if (this.ContainingType.Layout.Kind == LayoutKind.Explicit)
                {
                    // error CS0625: '<field>': instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
                    diagnostics.Add(ErrorCode.ERR_MissingStructOffset, this.ErrorLocation, this.AttributeOwner);
                }
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        internal sealed override bool HasSpecialName
        {
            get
            {
                if (this.HasRuntimeSpecialName)
                {
                    return true;
                }

                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        internal sealed override bool IsNotSerialized
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasNonSerializedAttribute;
            }
        }

        internal sealed override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null ? data.MarshallingInformation : null;
            }
        }

        internal sealed override int? TypeLayoutOffset
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null ? data.Offset : null;
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (this.RefKind == RefKind.RefReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }

            var compilation = this.DeclaringCompilation;
            var type = this.TypeWithAnnotations;

            if (type.Type.ContainsDynamic())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length));
            }

            if (compilation.ShouldEmitNativeIntegerAttributes(type.Type))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(this, type.Type));
            }

            if (type.Type.ContainsTupleNames())
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, ContainingType.GetNullableContextValue(), type));
            }
        }
    }
}
