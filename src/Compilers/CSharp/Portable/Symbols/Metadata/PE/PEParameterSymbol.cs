// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
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
            // |u|ss|fffffffff|n|rrr|cccccccc|vvvvvvvv|
            // 
            // v = decoded well known attribute values. 8 bits.
            // c = completion states for well known attributes. 1 if given attribute has been decoded, 0 otherwise. 8 bits.
            // r = RefKind. 3 bits.
            // n = hasNameInMetadata. 1 bit.
            // f = FlowAnalysisAnnotations. 9 bits (8 value bits + 1 completion bit).
            // s = Scope. 2 bits.
            // u = HasUnscopedRefAttribute. 1 bit.
            // Current total = 32 bits.

            private const int WellKnownAttributeDataOffset = 0;
            private const int WellKnownAttributeCompletionFlagOffset = 8;
            private const int RefKindOffset = 16;
            private const int FlowAnalysisAnnotationsOffset = 21;
            private const int ScopeOffset = 29;

            private const int RefKindMask = 0x7;
            private const int WellKnownAttributeDataMask = 0xFF;
            private const int WellKnownAttributeCompletionFlagMask = WellKnownAttributeDataMask;
            private const int FlowAnalysisAnnotationsMask = 0xFF;
            private const int ScopeMask = 0x3;

            private const int HasNameInMetadataBit = 0x1 << 19;
            private const int FlowAnalysisAnnotationsCompletionBit = 0x1 << 20;
            private const int HasUnscopedRefAttributeBit = 0x1 << 31;

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

            public ScopedKind Scope
            {
                get { return (ScopedKind)((_bits >> ScopeOffset) & ScopeMask); }
            }

            public bool HasUnscopedRefAttribute
            {
                get { return (_bits & HasUnscopedRefAttributeBit) != 0; }
            }

#if DEBUG
            static PackedFlags()
            {
                // Verify masks are sufficient for values.
                Debug.Assert(EnumUtilities.ContainsAllValues<WellKnownAttributeFlags>(WellKnownAttributeDataMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<RefKind>(RefKindMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<FlowAnalysisAnnotations>(FlowAnalysisAnnotationsMask));
                Debug.Assert(EnumUtilities.ContainsAllValues<ScopedKind>(ScopeMask));
            }
#endif

            public PackedFlags(RefKind refKind, bool attributesAreComplete, bool hasNameInMetadata, ScopedKind scope, bool hasUnscopedRefAttribute)
            {
                int refKindBits = ((int)refKind & RefKindMask) << RefKindOffset;
                int attributeBits = attributesAreComplete ? AllWellKnownAttributesCompleteNoData : 0;
                int hasNameInMetadataBits = hasNameInMetadata ? HasNameInMetadataBit : 0;
                int scopeBits = ((int)scope & ScopeMask) << ScopeOffset;
                int hasUnscopedRefAttributeBits = hasUnscopedRefAttribute ? HasUnscopedRefAttributeBit : 0;

                _bits = refKindBits | attributeBits | hasNameInMetadataBits | scopeBits | hasUnscopedRefAttributeBits;
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
        private ConstantValue? _lazyDefaultValue = ConstantValue.Unset;

        [Flags]
        private enum IsParamsValues : byte
        {
            NotInitialized = 0,
            Initialized = 1,
            Array = 2,
            Collection = 4,
        }

        private IsParamsValues _lazyIsParams;

        private static readonly ImmutableArray<int> s_defaultStringHandlerAttributeIndexes = ImmutableArray.Create(int.MinValue);
        private ImmutableArray<int> _lazyInterpolatedStringHandlerAttributeIndexes = s_defaultStringHandlerAttributeIndexes;

        /// <summary>
        /// The index of a CallerArgumentExpression. The value -2 means uninitialized, -1 means
        /// not found. Otherwise, the index of the CallerArgumentExpression.
        /// </summary>        
        private int _lazyCallerArgumentExpressionParameterIndex = -2;

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
            bool isReturn,
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
            ScopedKind scope = ScopedKind.None;
            bool hasUnscopedRefAttribute = false;

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
                _lazyIsParams = IsParamsValues.Initialized;
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
                    else if (!isReturn && moduleSymbol.Module.HasRequiresLocationAttribute(handle))
                    {
                        refKind = RefKind.RefReadOnlyParameter;
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

                if (moduleSymbol.TryDecodeExtensionErasureAttribute(handle, (PENamedTypeSymbol)containingSymbol.ContainingType, containingSymbol as PEMethodSymbol) is { } typeWithExtensions)
                {
                    typeWithAnnotations = TypeWithAnnotations.Create(typeWithExtensions);
                    // PROTOTYPE deal with tuple names, dynamic, etc
                    // PROTOTYPE consider checking that the type is the erased version of the decoded type
                }
                else
                {
                    var typeSymbol = DynamicTypeDecoder.TransformType(typeWithAnnotations.Type, countOfCustomModifiers, handle, moduleSymbol, refKind);
                    typeSymbol = NativeIntegerTypeDecoder.TransformType(typeSymbol, handle, moduleSymbol, containingSymbol.ContainingType);
                    typeWithAnnotations = typeWithAnnotations.WithTypeAndModifiers(typeSymbol, typeWithAnnotations.CustomModifiers);
                    // Decode nullable before tuple types to avoid converting between
                    // NamedTypeSymbol and TupleTypeSymbol unnecessarily.

                    // The containing type is passed to NullableTypeDecoder.TransformType to determine access
                    // for property parameters because the property does not have explicit accessibility in metadata.
                    var accessSymbol = containingSymbol.Kind == SymbolKind.Property ? containingSymbol.ContainingSymbol : containingSymbol;
                    typeWithAnnotations = NullableTypeDecoder.TransformType(typeWithAnnotations, handle, moduleSymbol, accessSymbol: accessSymbol, nullableContext: nullableContext);
                    typeWithAnnotations = TupleTypeDecoder.DecodeTupleTypesIfApplicable(typeWithAnnotations, handle, moduleSymbol);
                }

                hasUnscopedRefAttribute = _moduleSymbol.Module.HasUnscopedRefAttribute(_handle);
                if (hasUnscopedRefAttribute)
                {
                    if (_moduleSymbol.Module.HasScopedRefAttribute(_handle))
                    {
                        isBad = true;
                    }
                    scope = ScopedKind.None;
                }
                else if (_moduleSymbol.Module.HasScopedRefAttribute(_handle))
                {
                    if (isByRef)
                    {
                        Debug.Assert(refKind != RefKind.None);
                        scope = ScopedKind.ScopedRef;
                    }
                    else if (typeWithAnnotations.Type.IsRefLikeOrAllowsRefLikeType())
                    {
                        scope = ScopedKind.ScopedValue;
                    }
                    else
                    {
                        isBad = true;
                    }
                }
                else if (ParameterHelpers.IsRefScopedByDefault(_moduleSymbol.UseUpdatedEscapeRules, refKind))
                {
                    scope = ScopedKind.ScopedRef;
                }
            }

            _typeWithAnnotations = typeWithAnnotations;

            bool hasNameInMetadata = !string.IsNullOrEmpty(_name);
            if (!hasNameInMetadata)
            {
                // As was done historically, if the parameter doesn't have a name, we give it the name "value".
                _name = "value";
            }

            _packedFlags = new PackedFlags(refKind, attributesAreComplete: handle.IsNil, hasNameInMetadata: hasNameInMetadata, scope, hasUnscopedRefAttribute);

            Debug.Assert(refKind == this.RefKind);
            Debug.Assert(hasNameInMetadata == this.HasNameInMetadata);
            Debug.Assert(_name is not null);
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
                ? new PEParameterSymbol(moduleSymbol, containingSymbol, ordinal, isByRef, typeWithModifiers, handle, nullableContext, 0, isReturn: isReturn, out isBad)
                : new PEParameterSymbolWithCustomModifiers(moduleSymbol, containingSymbol, ordinal, isByRef, refCustomModifiers, typeWithModifiers, handle, nullableContext, isReturn: isReturn, out isBad);

            bool hasInAttributeModifier = parameter.RefCustomModifiers.HasInAttributeModifier();

            if (isReturn)
            {
                // A RefReadOnly return parameter should always have this modreq, and vice versa.
                Debug.Assert(parameter.RefKind != RefKind.RefReadOnlyParameter);
                isBad |= (parameter.RefKind == RefKind.RefReadOnly) != hasInAttributeModifier;
            }
            else if (parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter)
            {
                // An in/ref readonly parameter should not have this modreq, unless the containing symbol was virtual or abstract.
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
                bool isReturn,
                out bool isBad) :
                    base(moduleSymbol, containingSymbol, ordinal, isByRef, type, handle, nullableContext,
                         refCustomModifiers.NullToEmpty().Length + type.CustomModifiers.Length,
                         isReturn: isReturn, out isBad)
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

        public override int MetadataToken
        {
            get { return MetadataTokens.GetToken(_handle); }
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
        internal ConstantValue? ImportConstantValue(bool ignoreAttributes = false)
        {
            Debug.Assert(!_handle.IsNil);

            // Metadata Spec 22.33: 
            //   6. If Flags.HasDefault = 1 then this row [of Param table] shall own exactly one row in the Constant table [ERROR]
            //   7. If Flags.HasDefault = 0, then there shall be no rows in the Constant table owned by this row [ERROR]
            ConstantValue? value = null;

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

        internal override ConstantValue? ExplicitDefaultConstantValue
        {
            get
            {
                // The HasDefault flag has to be set, it doesn't suffice to mark the parameter with DefaultParameterValueAttribute.
                if (_lazyDefaultValue == ConstantValue.Unset)
                {
                    // From the C# point of view, there is no need to import a parameter's default value
                    // if the language isn't going to treat it as optional. However, we might need metadata constant value for NoPia.
                    // NOTE: Ignoring attributes for non-Optional parameters disrupts round-tripping, but the trade-off seems acceptable.
                    ConstantValue? value = ImportConstantValue(ignoreAttributes: !IsMetadataOptional);
                    Interlocked.CompareExchange(ref _lazyDefaultValue, value, ConstantValue.Unset);
                }

                return _lazyDefaultValue;
            }
        }

        private ConstantValue? GetDefaultDecimalOrDateTimeValue()
        {
            Debug.Assert(!_handle.IsNil);
            ConstantValue? value = null;

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
                        && ContainingAssembly.TypeConversions.HasCallerLineNumberConversion(this.Type, ref discardedUseSiteInfo);

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
                        && ContainingAssembly.TypeConversions.HasCallerInfoStringConversion(this.Type, ref discardedUseSiteInfo);

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
                        && ContainingAssembly.TypeConversions.HasCallerInfoStringConversion(this.Type, ref discardedUseSiteInfo);

                    value = _packedFlags.SetWellKnownAttribute(flag, isCallerMemberName);
                }
                return value;
            }
        }

        internal override int CallerArgumentExpressionParameterIndex
        {
            get
            {
                if (_lazyCallerArgumentExpressionParameterIndex != -2)
                {
                    return _lazyCallerArgumentExpressionParameterIndex;
                }

                var info = _moduleSymbol.Module.FindTargetAttribute(_handle, AttributeDescription.CallerArgumentExpressionAttribute);
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                bool isCallerArgumentExpression = info.HasValue
                    && !HasCallerLineNumberAttribute
                    && !HasCallerFilePathAttribute
                    && !HasCallerMemberNameAttribute
                    && ContainingAssembly.TypeConversions.HasCallerInfoStringConversion(this.Type, ref discardedUseSiteInfo);

                if (isCallerArgumentExpression)
                {
                    _moduleSymbol.Module.TryExtractStringValueFromAttribute(info.Handle, out var parameterName);
                    var parameters = ContainingSymbol.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].Name.Equals(parameterName, StringComparison.Ordinal))
                        {
                            _lazyCallerArgumentExpressionParameterIndex = i;
                            return i;
                        }
                    }
                }

                _lazyCallerArgumentExpressionParameterIndex = -1;
                return -1;
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

#nullable enable
        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes
        {
            get
            {
                EnsureInterpolatedStringHandlerArgumentAttributeDecoded();
                ImmutableArray<int> indexes = _lazyInterpolatedStringHandlerAttributeIndexes;
                Debug.Assert(indexes != s_defaultStringHandlerAttributeIndexes);
                return indexes.NullToEmpty();
            }
        }

        internal override bool HasInterpolatedStringHandlerArgumentError
        {
            get
            {
                EnsureInterpolatedStringHandlerArgumentAttributeDecoded();
                ImmutableArray<int> indexes = _lazyInterpolatedStringHandlerAttributeIndexes;
                Debug.Assert(indexes != s_defaultStringHandlerAttributeIndexes);
                return indexes.IsDefault;
            }
        }

        private void EnsureInterpolatedStringHandlerArgumentAttributeDecoded()
        {
            ImmutableArray<int> indexes = _lazyInterpolatedStringHandlerAttributeIndexes;
            if (indexes == s_defaultStringHandlerAttributeIndexes)
            {
                indexes = DecodeInterpolatedStringHandlerArgumentAttribute();
                Debug.Assert(indexes != s_defaultStringHandlerAttributeIndexes);
                var initialized = ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterpolatedStringHandlerAttributeIndexes, value: indexes, comparand: s_defaultStringHandlerAttributeIndexes);
                Debug.Assert(initialized == s_defaultStringHandlerAttributeIndexes || indexes == initialized || indexes.SequenceEqual(initialized));
            }
        }

        private ImmutableArray<int> DecodeInterpolatedStringHandlerArgumentAttribute()
        {
            var (paramNames, hasAttribute) = _moduleSymbol.Module.GetInterpolatedStringHandlerArgumentAttributeValues(_handle);

            if (!hasAttribute)
            {
                return ImmutableArray<int>.Empty;
            }
            else if (paramNames.IsDefault || Type is not NamedTypeSymbol { IsInterpolatedStringHandlerType: true })
            {
                return default;
            }

            if (paramNames.IsEmpty)
            {
                return ImmutableArray<int>.Empty;
            }

            var builder = ArrayBuilder<int>.GetInstance(paramNames.Length);
            var parameters = ContainingSymbol.GetParameters();

            foreach (var name in paramNames)
            {
                switch (name)
                {
                    case null:
                    case "" when !ContainingSymbol.RequiresInstanceReceiver() || ContainingSymbol is MethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.DelegateInvoke }:
                        // Invalid data, bail
                        builder.Free();
                        return default;

                    case "":
                        builder.Add(BoundInterpolatedStringArgumentPlaceholder.InstanceParameter);
                        break;

                    default:
                        var param = parameters.FirstOrDefault(static (p, name) => string.Equals(p.Name, name, StringComparison.Ordinal), name);
                        if (param is not null && (object)param != this)
                        {
                            builder.Add(param.Ordinal);
                            break;
                        }
                        else
                        {
                            builder.Free();
                            return default;
                        }
                }
            }

            return builder.ToImmutableAndFree();
        }
#nullable disable

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get
            {
                return _moduleSymbol.Module.GetStringValuesOfNotNullIfNotNullAttribute(_handle);
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

        public override bool IsParamsArray
        {
            get
            {
                return (GetIsParamsValues() & IsParamsValues.Array) != 0;
            }
        }

        public override bool IsParamsCollection
        {
            get
            {
                return (GetIsParamsValues() & IsParamsValues.Collection) != 0;
            }
        }

        private IsParamsValues GetIsParamsValues()
        {
            // This is also populated by loading attributes, but loading
            // attributes is more expensive, so we should only do it if
            // attributes are requested.
            if ((_lazyIsParams & IsParamsValues.Initialized) == 0)
            {
                IsParamsValues result = IsParamsValues.Initialized;

                if (_moduleSymbol.Module.HasParamArrayAttribute(_handle))
                {
                    result |= IsParamsValues.Array;
                }

                if (_moduleSymbol.Module.HasParamCollectionAttribute(_handle))
                {
                    result |= IsParamsValues.Collection;
                }

                _lazyIsParams = result;
            }

            return _lazyIsParams;
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

        internal sealed override ScopedKind EffectiveScope => _packedFlags.Scope;

        internal override bool HasUnscopedRefAttribute => _packedFlags.HasUnscopedRefAttribute;

        internal sealed override bool UseUpdatedEscapeRules => _moduleSymbol.UseUpdatedEscapeRules;

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                Debug.Assert(!_handle.IsNil);
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                // Filter out Params attributes if necessary and cache
                // the attribute handle for GetCustomAttributesToEmit
                bool filterOutParamArrayAttribute = ((_lazyIsParams & (IsParamsValues.Initialized | IsParamsValues.Array)) is 0 or (IsParamsValues.Initialized | IsParamsValues.Array));
                bool filterOutParamCollectionAttribute = ((_lazyIsParams & (IsParamsValues.Initialized | IsParamsValues.Collection)) is 0 or (IsParamsValues.Initialized | IsParamsValues.Collection));

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
                bool filterRequiresLocationAttribute = this.RefKind == RefKind.RefReadOnlyParameter;

                CustomAttributeHandle paramArrayAttribute;
                CustomAttributeHandle paramCollectionAttribute;
                CustomAttributeHandle constantAttribute;

                ImmutableArray<CSharpAttributeData> attributes =
                    containingPEModuleSymbol.GetCustomAttributesForToken(
                        _handle,
                        out paramArrayAttribute,
                        filterOutParamArrayAttribute ? AttributeDescription.ParamArrayAttribute : default,
                        out paramCollectionAttribute,
                        filterOutParamCollectionAttribute ? AttributeDescription.ParamCollectionAttribute : default,
                        out constantAttribute,
                        filterOutConstantAttributeDescription,
                        out _,
                        filterIsReadOnlyAttribute ? AttributeDescription.IsReadOnlyAttribute : default,
                        out _,
                        filterRequiresLocationAttribute ? AttributeDescription.RequiresLocationAttribute : default,
                        out _,
                        AttributeDescription.ScopedRefAttribute);

                if (!paramArrayAttribute.IsNil || !constantAttribute.IsNil || !paramCollectionAttribute.IsNil)
                {
                    var builder = ArrayBuilder<CSharpAttributeData>.GetInstance();

                    if (!paramArrayAttribute.IsNil)
                    {
                        builder.Add(new PEAttributeData(containingPEModuleSymbol, paramArrayAttribute));
                    }

                    if (!paramCollectionAttribute.IsNil)
                    {
                        builder.Add(new PEAttributeData(containingPEModuleSymbol, paramCollectionAttribute));
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

                if ((_lazyIsParams & IsParamsValues.Initialized) == 0)
                {
                    Debug.Assert(filterOutParamArrayAttribute);
                    Debug.Assert(filterOutParamCollectionAttribute);

                    IsParamsValues result = IsParamsValues.Initialized;

                    if (!paramArrayAttribute.IsNil)
                    {
                        result |= IsParamsValues.Array;
                    }

                    if (!paramCollectionAttribute.IsNil)
                    {
                        result |= IsParamsValues.Collection;
                    }

                    _lazyIsParams = result;
                }

                ImmutableInterlocked.InterlockedInitialize(
                    ref _lazyCustomAttributes,
                    attributes);
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

#nullable enable
        internal DiagnosticInfo? DeriveCompilerFeatureRequiredDiagnostic(MetadataDecoder decoder)
            => PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(this, (PEModuleSymbol)ContainingModule, Handle, allowedFeatures: CompilerFeatureRequiredFeatures.None, decoder);

        public override bool HasUnsupportedMetadata
        {
            get
            {
                var containingModule = (PEModuleSymbol)ContainingModule;
                var decoder = ContainingSymbol switch
                {
                    PEMethodSymbol method => new MetadataDecoder(containingModule, method),
                    PEPropertySymbol => new MetadataDecoder(containingModule, (PENamedTypeSymbol)ContainingType),
                    _ => throw ExceptionUtilities.UnexpectedValue(this.ContainingSymbol.Kind)
                };

                return DeriveCompilerFeatureRequiredDiagnostic(decoder) is { Code: (int)ErrorCode.ERR_UnsupportedCompilerFeature } || base.HasUnsupportedMetadata;
            }
        }
    }
}
