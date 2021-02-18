﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all method parameters imported from a PE/module.
    /// </summary>
    internal class PEParameterSymbol : ParameterSymbol
    {
        [Flags]
        private enum WellKnownAttributeFlags
        {
            HasIDispatchConstantAttribute = 0x1 << 0,
            HasIUnknownConstantAttribute = 0x1 << 1,
            HasCallerFilePathAttribute = 0x1 << 2,
            HasCallerLineNumberAttribute = 0x1 << 3,
            HasCallerMemberNameAttribute = 0x1 << 4,
            IsCallerFilePath = 0x1 << 5,
            IsCallerLineNumber = 0x1 << 6,
            IsCallerMemberName = 0x1 << 7,
        }

        private struct PackedFlags
        {
            // Layout:
            // |...|fffffffff|n|rr|cccccccc|vvvvvvvv|
            // 
            // v = decoded well known attribute values. 8 bits.
            // c = completion states for well known attributes. 1 if given attribute has been decoded, 0 otherwise. 8 bits.
            // r = RefKind. 2 bits.
            // n = hasNameInMetadata. 1 bit.
            // f = FlowAnalysisAnnotations. 9 bits (8 value bits + 1 completion bit).

            private const int WellKnownAttributeDataOffset = 0;
            private const int WellKnownAttributeCompletionFlagOffset = 8;
            private const int RefKindOffset = 16;
            private const int FlowAnalysisAnnotationsOffset = 20;

            private const int RefKindMask = 0x3;
            private const int WellKnownAttributeDataMask = 0xFF;
            private const int WellKnownAttributeCompletionFlagMask = WellKnownAttributeDataMask;
            private const int FlowAnalysisAnnotationsMask = 0xFF;

            private const int HasNameInMetadataBit = 0x1 << 18;
            private const int FlowAnalysisAnnotationsCompletionBit = 0x1 << 19;

            private const int AllWellKnownAttributesCompleteNoData = WellKnownAttributeCompletionFlagMask << WellKnownAttributeCompletionFlagOffset;

            private int _bits;

            public RefKind RefKind
            {
                get { return (RefKind)((_bits >> RefKindOffset) & RefKindMask); }
            }

            public bool HasNameInMetadata
            {
                get { return (_bits & HasNameInMetadataBit) != 0; }
            }

#if DEBUG
            static PackedFlags()
            {
                // Verify masks are sufficient for values.
                Debug.Assert(EnumUtilities.ContainsAllValues<WellKnownAttributeFlags>(WellKnownAttributeDataMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<RefKind>(RefKindMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<FlowAnalysisAnnotations>(FlowAnalysisAnnotationsMask));
            }
#endif

            public PackedFlags(RefKind refKind, bool attributesAreComplete, bool hasNameInMetadata)
            {
                int refKindBits = ((int)refKind & RefKindMask) << RefKindOffset;
                int attributeBits = attributesAreComplete ? AllWellKnownAttributesCompleteNoData : 0;
                int hasNameInMetadataBits = hasNameInMetadata ? HasNameInMetadataBit : 0;

                _bits = refKindBits | attributeBits | hasNameInMetadataBits;
            }

            public bool SetWellKnownAttribute(WellKnownAttributeFlags flag, bool value)
            {
                // a value has been decoded:
                int bitsToSet = (int)flag << WellKnownAttributeCompletionFlagOffset;
                if (value)
                {
                    // the actual value:
                    bitsToSet |= ((int)flag << WellKnownAttributeDataOffset);
                }

                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
                return value;
            }

            public bool TryGetWellKnownAttribute(WellKnownAttributeFlags flag, out bool value)
            {
                int theBits = _bits; // Read this.bits once to ensure the consistency of the value and completion flags.
                value = (theBits & ((int)flag << WellKnownAttributeDataOffset)) != 0;
                return (theBits & ((int)flag << WellKnownAttributeCompletionFlagOffset)) != 0;
            }

            public bool SetFlowAnalysisAnnotations(FlowAnalysisAnnotations value)
            {
                int bitsToSet = FlowAnalysisAnnotationsCompletionBit | (((int)value & FlowAnalysisAnnotationsMask) << FlowAnalysisAnnotationsOffset);
                return ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public bool TryGetFlowAnalysisAnnotations(out FlowAnalysisAnnotations value)
            {
                int theBits = _bits; // Read this.bits once to ensure the consistency of the value and completion flags.
                value = (FlowAnalysisAnnotations)((theBits >> FlowAnalysisAnnotationsOffset) & FlowAnalysisAnnotationsMask);
                var result = (theBits & FlowAnalysisAnnotationsCompletionBit) != 0;
                Debug.Assert(value == 0 || result);
                return result;
            }
        }

        private readonly Symbol _containingSymbol;
        private readonly string _name;
        private readonly TypeWithAnnotations _typeWithAnnotations;
        private readonly ParameterHandle _handle;
        private readonly ParameterAttributes _flags;
        private readonly PEModuleSymbol _moduleSymbol;

        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private ConstantValue _lazyDefaultValue = ConstantValue.Unset;
        private ThreeState _lazyIsParams;

        /// <summary>
        /// Attributes filtered out from m_lazyCustomAttributes, ParamArray, etc.
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyHiddenAttributes;

        private readonly ushort _ordinal;

        private PackedFlags _packedFlags;

        internal static PEParameterSymbol Create(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol containingSymbol,
            bool isContainingSymbolVirtual,
            int ordinal,
            ParamInfo<TypeSymbol> parameterInfo,
            Symbol nullableContext,
            bool isReturn,
            out bool isBad)
        {
            return Create(
                moduleSymbol, containingSymbol, isContainingSymbolVirtual, ordinal,
                parameterInfo.IsByRef, parameterInfo.RefCustomModifiers, parameterInfo.Type,
                parameterInfo.Handle, nullableContext, parameterInfo.CustomModifiers, isReturn, out isBad);
        }

        /// <summary>
        /// Construct a parameter symbol for a property loaded from metadata.
        /// </summary>
        /// <param name="moduleSymbol"></param>
        /// <param name="containingSymbol"></param>
        /// <param name="ordinal"></param>
        /// <param name="handle">The property parameter doesn't have a name in metadata,
        /// so this is the handle of a corresponding accessor parameter, if there is one,
        /// or of the ParamInfo passed in, otherwise.</param>
        /// <param name="parameterInfo" />
        /// <param name="isBad" />
        internal static PEParameterSymbol Create(
            PEModuleSymbol moduleSymbol,
            PEPropertySymbol containingSymbol,
            bool isContainingSymbolVirtual,
            int ordinal,
            ParameterHandle handle,
            ParamInfo<TypeSymbol> parameterInfo,
            Symbol nullableContext,
            out bool isBad)
        {
            return Create(
                moduleSymbol, containingSymbol, isContainingSymbolVirtual, ordinal,
                parameterInfo.IsByRef, parameterInfo.RefCustomModifiers, parameterInfo.Type,
                handle, nullableContext, parameterInfo.CustomModifiers, isReturn: false, out isBad);
        }

        private PEParameterSymbol(
            PEModuleSymbol moduleSymbol,
            Symbol containingSymbol,
            int ordinal,
            bool isByRef,
            TypeWithAnnotations typeWithAnnotations,
            ParameterHandle handle,
            Symbol nullableContext,
            int countOfCustomModifiers,
            out bool isBad)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert(typeWithAnnotations.HasType);

            isBad = false;
            _moduleSymbol = moduleSymbol;
            _containingSymbol = containingSymbol;
            _ordinal = (ushort)ordinal;

            _handle = handle;

            RefKind refKind = RefKind.None;

            if (handle.IsNil)
            {
                refKind = isByRef ? RefKind.Ref : RefKind.None;
                byte? value = nullableContext.GetNullableContextValue();
                if (value.HasValue)
                {
                    typeWithAnnotations = NullableTypeDecoder.TransformType(typeWithAnnotations, value.GetValueOrDefault(), default);
                }
                _lazyCustomAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                _lazyHiddenAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                _lazyDefaultValue = ConstantValue.NotAvailable;
                _lazyIsParams = ThreeState.False;
            }
            else
            {
                try
                {
                    moduleSymbol.Module.GetParamPropsOrThrow(handle, out _name, out _flags);
                }
                catch (BadImageFormatException)
                {
                    isBad = true;
                }

                if (isByRef)
                {
                    ParameterAttributes inOutFlags = _flags & (ParameterAttributes.Out | ParameterAttributes.In);

                    if (inOutFlags == ParameterAttributes.Out)
                    {
                        refKind = RefKind.Out;
                    }
                    else if (moduleSymbol.Module.HasIsReadOnlyAttribute(handle))
                    {
                        refKind = RefKind.In;
                    }
                    else
                    {
                        refKind = RefKind.Ref;
                    }
                }

                var typeSymbol = DynamicTypeDecoder.TransformType(typeWithAnnotations.Type, countOfCustomModifiers, handle, moduleSymbol, refKind);
                typeSymbol = NativeIntegerTypeDecoder.TransformType(typeSymbol, handle, moduleSymbol);
                typeWithAnnotations = typeWithAnnotations.WithTypeAndModifiers(typeSymbol, typeWithAnnotations.CustomModifiers);
                // Decode nullable before tuple types to avoid converting between
                // NamedTypeSymbol and TupleTypeSymbol unnecessarily.

                // The containing type is passed to NullableTypeDecoder.TransformType to determine access
                // for property parameters because the property does not have explicit accessibility in metadata.
                var accessSymbol = containingSymbol.Kind == SymbolKind.Property ? containingSymbol.ContainingSymbol : containingSymbol;
                typeWithAnnotations = NullableTypeDecoder.TransformType(typeWithAnnotations, handle, moduleSymbol, accessSymbol: accessSymbol, nullableContext: nullableContext);
                typeWithAnnotations = TupleTypeDecoder.DecodeTupleTypesIfApplicable(typeWithAnnotations, handle, moduleSymbol);
            }

            _typeWithAnnotations = typeWithAnnotations;

            bool hasNameInMetadata = !string.IsNullOrEmpty(_name);
            if (!hasNameInMetadata)
            {
                // As was done historically, if the parameter doesn't have a name, we give it the name "value".
                _name = "value";
            }

            _packedFlags = new PackedFlags(refKind, attributesAreComplete: handle.IsNil, hasNameInMetadata: hasNameInMetadata);

            Debug.Assert(refKind == this.RefKind);
            Debug.Assert(hasNameInMetadata == this.HasNameInMetadata);
        }

        private bool HasNameInMetadata
        {
            get
            {
                return _packedFlags.HasNameInMetadata;
            }
        }

        private static PEParameterSymbol Create(
            PEModuleSymbol moduleSymbol,
            Symbol containingSymbol,
            bool isContainingSymbolVirtual,
            int ordinal,
            bool isByRef,
            ImmutableArray<ModifierInfo<TypeSymbol>> refCustomModifiers,
            TypeSymbol type,
            ParameterHandle handle,
            Symbol nullableContext,
            ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers,
            bool isReturn,
            out bool isBad)
        {
            // We start without annotation (they will be decoded below)
            var typeWithModifiers = TypeWithAnnotations.Create(type, customModifiers: CSharpCustomModifier.Convert(customModifiers));

            PEParameterSymbol parameter = customModifiers.IsDefaultOrEmpty && refCustomModifiers.IsDefaultOrEmpty
                ? new PEParameterSymbol(moduleSymbol, containingSymbol, ordinal, isByRef, typeWithModifiers, handle, nullableContext, 0, out isBad)
                : new PEParameterSymbolWithCustomModifiers(moduleSymbol, containingSymbol, ordinal, isByRef, refCustomModifiers, typeWithModifiers, handle, nullableContext, out isBad);

            bool hasInAttributeModifier = parameter.RefCustomModifiers.HasInAttributeModifier();

            if (isReturn)
            {
                // A RefReadOnly return parameter should always have this modreq, and vice versa.
                isBad |= (parameter.RefKind == RefKind.RefReadOnly) != hasInAttributeModifier;
            }
            else if (parameter.RefKind == RefKind.In)
            {
                // An in parameter should not have this modreq, unless the containing symbol was virtual or abstract.
                isBad |= isContainingSymbolVirtual != hasInAttributeModifier;
            }
            else if (hasInAttributeModifier)
            {
                // This modreq should not exist on non-in parameters.
                isBad = true;
            }

            return parameter;
        }

        private sealed class PEParameterSymbolWithCustomModifiers : PEParameterSymbol
        {
            private readonly ImmutableArray<CustomModifier> _refCustomModifiers;

            public PEParameterSymbolWithCustomModifiers(
                PEModuleSymbol moduleSymbol,
                Symbol containingSymbol,
                int ordinal,
                bool isByRef,
                ImmutableArray<ModifierInfo<TypeSymbol>> refCustomModifiers,
                TypeWithAnnotations type,
                ParameterHandle handle,
                Symbol nullableContext,
                out bool isBad) :
                    base(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, nullableContext,
                         refCustomModifiers.NullToEmpty().Length + type.CustomModifiers.Length,
                         out isBad)
            {
                _refCustomModifiers = CSharpCustomModifier.Convert(refCustomModifiers);

                Debug.Assert(_refCustomModifiers.IsEmpty || isByRef);
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get
                {
                    return _refCustomModifiers;
                }
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return _packedFlags.RefKind;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override string MetadataName
        {
            get
            {
                return HasNameInMetadata ? _name : string.Empty;
            }
        }

        internal ParameterAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        public override bool IsDiscard
        {
            get
            {
                return false;
            }
        }

        // might be Nil
        internal ParameterHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingSymbol;
            }
        }

        internal override bool HasMetadataConstantValue
        {
            get
            {
                return (_flags & ParameterAttributes.HasDefault) != 0;
            }
        }

        /// <remarks>
        /// Internal for testing.  Non-test code should use <see cref="ExplicitDefaultConstantValue"/>.
        /// </remarks>
        internal ConstantValue ImportConstantValue(bool ignoreAttributes = false)
        {
            Debug.Assert(!_handle.IsNil);

            // Metadata Spec 22.33: 
            //   6. If Flags.HasDefault = 1 then this row [of Param table] shall own exactly one row in the Constant table [ERROR]
            //   7. If Flags.HasDefault = 0, then there shall be no rows in the Constant table owned by this row [ERROR]
            ConstantValue value = null;

            if ((_flags & ParameterAttributes.HasDefault) != 0)
            {
                value = _moduleSymbol.Module.GetParamDefaultValue(_handle);
            }

            if (value == null && !ignoreAttributes)
            {
                value = GetDefaultDecimalOrDateTimeValue();
            }

            return value;
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                // The HasDefault flag has to be set, it doesn't suffice to mark the parameter with DefaultParameterValueAttribute.
                if (_lazyDefaultValue == ConstantValue.Unset)
                {
                    // From the C# point of view, there is no need to import a parameter's default value
                    // if the language isn't going to treat it as optional. However, we might need metadata constant value for NoPia.
                    // NOTE: Ignoring attributes for non-Optional parameters disrupts round-tripping, but the trade-off seems acceptable.
                    ConstantValue value = ImportConstantValue(ignoreAttributes: !IsMetadataOptional);
                    Interlocked.CompareExchange(ref _lazyDefaultValue, value, ConstantValue.Unset);
                }

                return _lazyDefaultValue;
            }
        }

        private ConstantValue GetDefaultDecimalOrDateTimeValue()
        {
            Debug.Assert(!_handle.IsNil);
            ConstantValue value = null;

            // It is possible in Visual Basic for a parameter of object type to have a default value of DateTime type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (_moduleSymbol.Module.HasDateTimeConstantAttribute(_handle, out value))
            {
                return value;
            }

            // It is possible in Visual Basic for a parameter of object type to have a default value of decimal type.
            // If it's present, use it.  We'll let the call-site figure out whether it can actually be used.
            if (_moduleSymbol.Module.HasDecimalConstantAttribute(_handle, out value))
            {
                return value;
            }

            return value;
        }

        internal override bool IsMetadataOptional
        {
            get
            {
                return (_flags & ParameterAttributes.Optional) != 0;
            }
        }

        internal override bool IsIDispatchConstant
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasIDispatchConstantAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.IDispatchConstantAttribute));
                }
                return value;
            }
        }

        internal override bool IsIUnknownConstant
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasIUnknownConstantAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.IUnknownConstantAttribute));
                }
                return value;
            }
        }

        private bool HasCallerLineNumberAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerLineNumberAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.CallerLineNumberAttribute));
                }
                return value;
            }
        }

        private bool HasCallerFilePathAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerFilePathAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.CallerFilePathAttribute));
                }
                return value;
            }
        }

        private bool HasCallerMemberNameAttribute
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.HasCallerMemberNameAttribute;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    value = _packedFlags.SetWellKnownAttribute(flag, _moduleSymbol.Module.HasAttribute(_handle,
                        AttributeDescription.CallerMemberNameAttribute));
                }
                return value;
            }
        }

        internal override bool IsCallerLineNumber
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerLineNumber;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    bool isCallerLineNumber = HasCallerLineNumberAttribute
                        && new TypeConversions(ContainingAssembly).HasCallerLineNumberConversion(this.Type, ref discardedUseSiteInfo);

                    value = _packedFlags.SetWellKnownAttribute(flag, isCallerLineNumber);
                }
                return value;
            }
        }

        internal override bool IsCallerFilePath
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerFilePath;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    bool isCallerFilePath = !HasCallerLineNumberAttribute
                        && HasCallerFilePathAttribute
                        && new TypeConversions(ContainingAssembly).HasCallerInfoStringConversion(this.Type, ref discardedUseSiteInfo);

                    value = _packedFlags.SetWellKnownAttribute(flag, isCallerFilePath);
                }
                return value;
            }
        }

        internal override bool IsCallerMemberName
        {
            get
            {
                const WellKnownAttributeFlags flag = WellKnownAttributeFlags.IsCallerMemberName;

                bool value;
                if (!_packedFlags.TryGetWellKnownAttribute(flag, out value))
                {
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    bool isCallerMemberName = !HasCallerLineNumberAttribute
                        && !HasCallerFilePathAttribute
                        && HasCallerMemberNameAttribute
                        && new TypeConversions(ContainingAssembly).HasCallerInfoStringConversion(this.Type, ref discardedUseSiteInfo);

                    value = _packedFlags.SetWellKnownAttribute(flag, isCallerMemberName);
                }
                return value;
            }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                FlowAnalysisAnnotations value;
                if (!_packedFlags.TryGetFlowAnalysisAnnotations(out value))
                {
                    value = DecodeFlowAnalysisAttributes(_moduleSymbol.Module, _handle);
                    _packedFlags.SetFlowAnalysisAnnotations(value);
                }
                return value;
            }
        }

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(PEModule module, ParameterHandle handle)
        {
            FlowAnalysisAnnotations annotations = FlowAnalysisAnnotations.None;
            if (module.HasAttribute(handle, AttributeDescription.AllowNullAttribute)) annotations |= FlowAnalysisAnnotations.AllowNull;
            if (module.HasAttribute(handle, AttributeDescription.DisallowNullAttribute)) annotations |= FlowAnalysisAnnotations.DisallowNull;

            if (module.HasAttribute(handle, AttributeDescription.MaybeNullAttribute))
            {
                annotations |= FlowAnalysisAnnotations.MaybeNull;
            }
            else if (module.HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(handle, AttributeDescription.MaybeNullWhenAttribute, out bool when))
            {
                annotations |= (when ? FlowAnalysisAnnotations.MaybeNullWhenTrue : FlowAnalysisAnnotations.MaybeNullWhenFalse);
            }

            if (module.HasAttribute(handle, AttributeDescription.NotNullAttribute))
            {
                annotations |= FlowAnalysisAnnotations.NotNull;
            }
            else if (module.HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(handle, AttributeDescription.NotNullWhenAttribute, out bool when))
            {
                annotations |= (when ? FlowAnalysisAnnotations.NotNullWhenTrue : FlowAnalysisAnnotations.NotNullWhenFalse);
            }

            if (module.HasMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(handle, AttributeDescription.DoesNotReturnIfAttribute, out bool condition))
            {
                annotations |= (condition ? FlowAnalysisAnnotations.DoesNotReturnIfTrue : FlowAnalysisAnnotations.DoesNotReturnIfFalse);
            }

            return annotations;
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get
            {
                var attributes = GetAttributes();
                var result = ImmutableHashSet<string>.Empty;
                foreach (var attribute in attributes)
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullIfNotNullAttribute))
                    {
                        if (attribute.DecodeNotNullIfNotNullAttribute() is string parameterName)
                        {
                            result = result.Add(parameterName);
                        }
                    }
                }

                return result;
            }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                return _typeWithAnnotations;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        internal override bool IsMetadataIn
        {
            get { return (_flags & ParameterAttributes.In) != 0; }
        }

        internal override bool IsMetadataOut
        {
            get { return (_flags & ParameterAttributes.Out) != 0; }
        }

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return (_flags & ParameterAttributes.HasFieldMarshal) != 0;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                // the compiler doesn't need full marshalling information, just the unmanaged type or descriptor
                return null;
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((_flags & ParameterAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                Debug.Assert(!_handle.IsNil);
                return _moduleSymbol.Module.GetMarshallingDescriptor(_handle);
            }
        }

        internal override UnmanagedType MarshallingType
        {
            get
            {
                if ((_flags & ParameterAttributes.HasFieldMarshal) == 0)
                {
                    return 0;
                }

                Debug.Assert(!_handle.IsNil);
                return _moduleSymbol.Module.GetMarshallingType(_handle);
            }
        }

        public override bool IsParams
        {
            get
            {
                // This is also populated by loading attributes, but loading
                // attributes is more expensive, so we should only do it if
                // attributes are requested.
                if (!_lazyIsParams.HasValue())
                {
                    _lazyIsParams = _moduleSymbol.Module.HasParamsAttribute(_handle).ToThreeState();
                }
                return _lazyIsParams.Value();
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingSymbol.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                Debug.Assert(!_handle.IsNil);
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                // Filter out ParamArrayAttributes if necessary and cache
                // the attribute handle for GetCustomAttributesToEmit
                bool filterOutParamArrayAttribute = (!_lazyIsParams.HasValue() || _lazyIsParams.Value());

                ConstantValue defaultValue = this.ExplicitDefaultConstantValue;
                AttributeDescription filterOutConstantAttributeDescription = default(AttributeDescription);

                if ((object)defaultValue != null)
                {
                    if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.DateTime)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DateTimeConstantAttribute;
                    }
                    else if (defaultValue.Discriminator == ConstantValueTypeDiscriminator.Decimal)
                    {
                        filterOutConstantAttributeDescription = AttributeDescription.DecimalConstantAttribute;
                    }
                }

                bool filterIsReadOnlyAttribute = this.RefKind == RefKind.In;

                if (filterOutParamArrayAttribute || filterOutConstantAttributeDescription.Signatures != null || filterIsReadOnlyAttribute)
                {
                    CustomAttributeHandle paramArrayAttribute;
                    CustomAttributeHandle constantAttribute;
                    CustomAttributeHandle isReadOnlyAttribute;

                    ImmutableArray<CSharpAttributeData> attributes =
                        containingPEModuleSymbol.GetCustomAttributesForToken(
                            _handle,
                            out paramArrayAttribute,
                            filterOutParamArrayAttribute ? AttributeDescription.ParamArrayAttribute : default,
                            out constantAttribute,
                            filterOutConstantAttributeDescription,
                            out isReadOnlyAttribute,
                            filterIsReadOnlyAttribute ? AttributeDescription.IsReadOnlyAttribute : default,
                            out _,
                            default);

                    if (!paramArrayAttribute.IsNil || !constantAttribute.IsNil)
                    {
                        var builder = ArrayBuilder<CSharpAttributeData>.GetInstance();

                        if (!paramArrayAttribute.IsNil)
                        {
                            builder.Add(new PEAttributeData(containingPEModuleSymbol, paramArrayAttribute));
                        }

                        if (!constantAttribute.IsNil)
                        {
                            builder.Add(new PEAttributeData(containingPEModuleSymbol, constantAttribute));
                        }

                        ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, builder.ToImmutableAndFree());
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, ImmutableArray<CSharpAttributeData>.Empty);
                    }

                    if (!_lazyIsParams.HasValue())
                    {
                        Debug.Assert(filterOutParamArrayAttribute);
                        _lazyIsParams = (!paramArrayAttribute.IsNil).ToThreeState();
                    }

                    ImmutableInterlocked.InterlockedInitialize(
                        ref _lazyCustomAttributes,
                        attributes);
                }
                else
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyHiddenAttributes, ImmutableArray<CSharpAttributeData>.Empty);
                    containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
                }
            }

            Debug.Assert(!_lazyHiddenAttributes.IsDefault);
            return _lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            foreach (CSharpAttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            // Yield hidden attributes last, order might be important.
            foreach (CSharpAttributeData attribute in _lazyHiddenAttributes)
            {
                yield return attribute;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        public sealed override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            return other is NativeIntegerParameterSymbol nps ?
                nps.Equals(this, compareKind) :
                base.Equals(other, compareKind);
        }
    }
}
