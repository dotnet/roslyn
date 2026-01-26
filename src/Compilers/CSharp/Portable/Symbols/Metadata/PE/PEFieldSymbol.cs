// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all fields imported from a PE/module.
    /// </summary>
    internal sealed class PEFieldSymbol : FieldSymbol
    {
        private struct PackedFlags
        {
            // Layout:
            // |........................|qq|rr|v|fffff|
            //
            // f = FlowAnalysisAnnotations. 5 bits (4 value bits + 1 completion bit).
            // v = IsVolatile 1 bit
            // r = RefKind 2 bits
            // q = Required members. 2 bits (1 value bit + 1 completion bit).

            private const int HasDisallowNullAttribute = 0x1 << 0;
            private const int HasAllowNullAttribute = 0x1 << 1;
            private const int HasMaybeNullAttribute = 0x1 << 2;
            private const int HasNotNullAttribute = 0x1 << 3;
            private const int FlowAnalysisAnnotationsCompletionBit = 0x1 << 4;
            private const int IsVolatileBit = 0x1 << 5;
            private const int RefKindOffset = 6;
            private const int RefKindMask = 0x3;

            private const int HasRequiredMemberAttribute = 0x1 << 8;
            private const int RequiredMemberCompletionBit = 0x1 << 9;

            private int _bits;

            public bool SetFlowAnalysisAnnotations(FlowAnalysisAnnotations value)
            {
                Debug.Assert((value & ~(FlowAnalysisAnnotations.DisallowNull | FlowAnalysisAnnotations.AllowNull | FlowAnalysisAnnotations.MaybeNull | FlowAnalysisAnnotations.NotNull)) == 0);

                int bitsToSet = FlowAnalysisAnnotationsCompletionBit;
                if ((value & FlowAnalysisAnnotations.DisallowNull) != 0) bitsToSet |= PackedFlags.HasDisallowNullAttribute;
                if ((value & FlowAnalysisAnnotations.AllowNull) != 0) bitsToSet |= PackedFlags.HasAllowNullAttribute;
                if ((value & FlowAnalysisAnnotations.MaybeNull) != 0) bitsToSet |= PackedFlags.HasMaybeNullAttribute;
                if ((value & FlowAnalysisAnnotations.NotNull) != 0) bitsToSet |= PackedFlags.HasNotNullAttribute;

                return ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public bool TryGetFlowAnalysisAnnotations(out FlowAnalysisAnnotations value)
            {
                int theBits = _bits; // Read this.bits once to ensure the consistency of the value and completion flags.
                value = FlowAnalysisAnnotations.None;
                if ((theBits & PackedFlags.HasDisallowNullAttribute) != 0) value |= FlowAnalysisAnnotations.DisallowNull;
                if ((theBits & PackedFlags.HasAllowNullAttribute) != 0) value |= FlowAnalysisAnnotations.AllowNull;
                if ((theBits & PackedFlags.HasMaybeNullAttribute) != 0) value |= FlowAnalysisAnnotations.MaybeNull;
                if ((theBits & PackedFlags.HasNotNullAttribute) != 0) value |= FlowAnalysisAnnotations.NotNull;

                var result = (theBits & FlowAnalysisAnnotationsCompletionBit) != 0;
                Debug.Assert(value == 0 || result);
                return result;
            }

            public void SetIsVolatile(bool isVolatile)
            {
                if (isVolatile) ThreadSafeFlagOperations.Set(ref _bits, IsVolatileBit);
                Debug.Assert(IsVolatile == isVolatile);
            }

            public bool IsVolatile => (_bits & IsVolatileBit) != 0;

            public void SetRefKind(RefKind refKind)
            {
                int bits = ((int)refKind & RefKindMask) << RefKindOffset;
                if (bits != 0) ThreadSafeFlagOperations.Set(ref _bits, bits);
                Debug.Assert(RefKind == refKind);
            }

            public RefKind RefKind => (RefKind)((_bits >> RefKindOffset) & RefKindMask);

            public bool SetHasRequiredMemberAttribute(bool isRequired)
            {
                int bitsToSet = RequiredMemberCompletionBit | (isRequired ? HasRequiredMemberAttribute : 0);
                return ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public bool TryGetHasRequiredMemberAttribute(out bool hasRequiredMemberAttribute)
            {
                if ((_bits & RequiredMemberCompletionBit) != 0)
                {
                    hasRequiredMemberAttribute = (_bits & HasRequiredMemberAttribute) != 0;
                    return true;
                }

                hasRequiredMemberAttribute = false;
                return false;
            }
        }

        private readonly FieldDefinitionHandle _handle;
        private readonly string _name;
        private readonly FieldAttributes _flags;
        private readonly PENamedTypeSymbol _containingType;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private ConstantValue _lazyConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset; // Indicates an uninitialized ConstantValue
        private Tuple<CultureInfo, string> _lazyDocComment;
        private CachedUseSiteInfo<AssemblySymbol> _lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        private TypeWithAnnotations.Boxed _lazyType;
        private int _lazyFixedSize;
        private NamedTypeSymbol _lazyFixedImplementationType;
        private PEEventSymbol _associatedEventOpt;
        private PackedFlags _packedFlags;
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;

        internal PEFieldSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            FieldDefinitionHandle fieldDef)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!fieldDef.IsNil);

            _handle = fieldDef;
            _containingType = containingType;
            _packedFlags = new PackedFlags();

            try
            {
                moduleSymbol.Module.GetFieldDefPropsOrThrow(fieldDef, out _name, out _flags);
            }
            catch (BadImageFormatException)
            {
                if ((object)_name == null)
                {
                    _name = String.Empty;
                }

                _lazyCachedUseSiteInfo.Initialize(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override int MetadataToken
        {
            get { return MetadataTokens.GetToken(_handle); }
        }

        internal FieldAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (_flags & FieldAttributes.SpecialName) != 0;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (_flags & FieldAttributes.RTSpecialName) != 0;
            }
        }

        internal override bool IsNotSerialized
        {
            get
            {
#pragma warning disable SYSLIB0050 // 'TypeAttributes.Serializable' is obsolete
                return (_flags & FieldAttributes.NotSerialized) != 0;
#pragma warning restore SYSLIB0050
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

        internal override bool IsMarshalledExplicitly
        {
            get
            {
                return ((_flags & FieldAttributes.HasFieldMarshal) != 0);
            }
        }

        internal override UnmanagedType MarshallingType
        {
            get
            {
                if ((_flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return 0;
                }

                return _containingType.ContainingPEModule.Module.GetMarshallingType(_handle);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                if ((_flags & FieldAttributes.HasFieldMarshal) == 0)
                {
                    return default(ImmutableArray<byte>);
                }

                return _containingType.ContainingPEModule.Module.GetMarshallingDescriptor(_handle);
            }
        }

        internal override int? TypeLayoutOffset
        {
            get
            {
                return _containingType.ContainingPEModule.Module.GetFieldOffset(_handle);
            }
        }

        internal FieldDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        /// <summary>
        /// Mark this field as the backing field of a field-like event.
        /// The caller will also ensure that it is excluded from the member list of
        /// the containing type (as it would be in source).
        /// </summary>
        internal void SetAssociatedEvent(PEEventSymbol eventSymbol)
        {
            Debug.Assert((object)eventSymbol != null);
            Debug.Assert(TypeSymbol.Equals(eventSymbol.ContainingType, _containingType, TypeCompareKind.ConsiderEverything2));

            // This should always be true in valid metadata - there should only
            // be one event with a given name in a given type.
            if ((object)_associatedEventOpt == null)
            {
                // No locking required since this method will only be called by the thread that created
                // the field symbol (and will be called before the field symbol is added to the containing 
                // type members and available to other threads).
                _associatedEventOpt = eventSymbol;
            }
        }

        private void EnsureSignatureIsLoaded()
        {
            if (_lazyType == null)
            {
                var moduleSymbol = _containingType.ContainingPEModule;
                FieldInfo<TypeSymbol> fieldInfo = new MetadataDecoder(moduleSymbol, _containingType).DecodeFieldSignature(_handle);
                TypeSymbol typeSymbol = fieldInfo.Type;
                ImmutableArray<CustomModifier> customModifiersArray = CSharpCustomModifier.Convert(fieldInfo.CustomModifiers);

                typeSymbol = DynamicTypeDecoder.TransformType(typeSymbol, customModifiersArray.Length, _handle, moduleSymbol);
                typeSymbol = NativeIntegerTypeDecoder.TransformType(typeSymbol, _handle, moduleSymbol, _containingType);

                // We start without annotations
                var type = TypeWithAnnotations.Create(typeSymbol, customModifiers: customModifiersArray);

                // Decode nullable before tuple types to avoid converting between
                // NamedTypeSymbol and TupleTypeSymbol unnecessarily.
                type = NullableTypeDecoder.TransformType(type, _handle, moduleSymbol, accessSymbol: this, nullableContext: _containingType);
                type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, _handle, moduleSymbol);

                RefKind refKind = fieldInfo.IsByRef ?
                    moduleSymbol.Module.HasIsReadOnlyAttribute(_handle) ? RefKind.RefReadOnly : RefKind.Ref :
                    RefKind.None;
                _packedFlags.SetRefKind(refKind);
                _packedFlags.SetIsVolatile(customModifiersArray.Any(static m => !m.IsOptional && ((CSharpCustomModifier)m).ModifierSymbol.SpecialType == SpecialType.System_Runtime_CompilerServices_IsVolatile));

                TypeSymbol fixedElementType;
                int fixedSize;
                if (customModifiersArray.IsEmpty && IsFixedBuffer(out fixedSize, out fixedElementType))
                {
                    _lazyFixedSize = fixedSize;
                    _lazyFixedImplementationType = type.Type as NamedTypeSymbol;
                    type = TypeWithAnnotations.Create(new PointerTypeSymbol(TypeWithAnnotations.Create(fixedElementType)));
                }

                ImmutableInterlocked.InterlockedInitialize(ref _lazyRefCustomModifiers, CSharpCustomModifier.Convert(fieldInfo.RefCustomModifiers));

                Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type), null);
            }
        }

        private bool IsFixedBuffer(out int fixedSize, out TypeSymbol fixedElementType)
        {
            fixedSize = 0;
            fixedElementType = null;

            string elementTypeName;
            int bufferSize;
            PEModuleSymbol containingPEModule = this.ContainingPEModule;
            if (containingPEModule.Module.HasFixedBufferAttribute(_handle, out elementTypeName, out bufferSize))
            {
                var decoder = new MetadataDecoder(containingPEModule);
                var elementType = decoder.GetTypeSymbolForSerializedType(elementTypeName);
                if (elementType.FixedBufferElementSizeInBytes() != 0)
                {
                    fixedSize = bufferSize;
                    fixedElementType = elementType;
                    return true;
                }
            }

            return false;
        }

        private PEModuleSymbol ContainingPEModule
        {
            get
            {
                return ((PENamespaceSymbol)ContainingNamespace).ContainingPEModule;
            }
        }

        public override RefKind RefKind
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _packedFlags.RefKind;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyRefCustomModifiers;
            }
        }

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            EnsureSignatureIsLoaded();
            return _lazyType.Value;
        }

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                FlowAnalysisAnnotations value;
                if (!_packedFlags.TryGetFlowAnalysisAnnotations(out value))
                {
                    value = DecodeFlowAnalysisAttributes(_containingType.ContainingPEModule.Module, _handle);
                    _packedFlags.SetFlowAnalysisAnnotations(value);
                }
                return value;
            }
        }

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(PEModule module, FieldDefinitionHandle handle)
        {
            FlowAnalysisAnnotations annotations = FlowAnalysisAnnotations.None;
            if (module.HasAttribute(handle, AttributeDescription.AllowNullAttribute)) annotations |= FlowAnalysisAnnotations.AllowNull;
            if (module.HasAttribute(handle, AttributeDescription.DisallowNullAttribute)) annotations |= FlowAnalysisAnnotations.DisallowNull;
            if (module.HasAttribute(handle, AttributeDescription.MaybeNullAttribute)) annotations |= FlowAnalysisAnnotations.MaybeNull;
            if (module.HasAttribute(handle, AttributeDescription.NotNullAttribute)) annotations |= FlowAnalysisAnnotations.NotNull;
            return annotations;
        }

        public override bool IsFixedSizeBuffer
        {
            get
            {
                EnsureSignatureIsLoaded();
                return (object)_lazyFixedImplementationType != null;
            }
        }

        public override int FixedSize
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _lazyFixedSize;
            }
        }

        internal override NamedTypeSymbol FixedImplementationType(PEModuleBuilder emitModule)
        {
            EnsureSignatureIsLoaded();
            return _lazyFixedImplementationType;
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _associatedEventOpt;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return (_flags & FieldAttributes.InitOnly) != 0;
            }
        }

        public override bool IsVolatile
        {
            get
            {
                EnsureSignatureIsLoaded();
                return _packedFlags.IsVolatile;
            }
        }

        public override bool IsConst
        {
            get
            {
                return (_flags & FieldAttributes.Literal) != 0 || GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false) != null;
            }
        }

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            if (_lazyConstantValue == Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                ConstantValue value = null;

                if ((_flags & FieldAttributes.Literal) != 0)
                {
                    value = _containingType.ContainingPEModule.Module.GetConstantFieldValue(_handle);
                }

                // If this is a Decimal, the constant value may come from DecimalConstantAttribute

                if (this.Type.SpecialType == SpecialType.System_Decimal)
                {
                    ConstantValue defaultValue;

                    if (_containingType.ContainingPEModule.Module.HasDecimalConstantAttribute(Handle, out defaultValue))
                    {
                        value = defaultValue;
                    }
                }

                Interlocked.CompareExchange(
                    ref _lazyConstantValue,
                    value,
                    Microsoft.CodeAnalysis.ConstantValue.Unset);
            }

            return _lazyConstantValue;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
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
            get
            {
                var access = Accessibility.Private;

                switch (_flags & FieldAttributes.FieldAccessMask)
                {
                    case FieldAttributes.Assembly:
                        access = Accessibility.Internal;
                        break;

                    case FieldAttributes.FamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case FieldAttributes.FamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case FieldAttributes.Private:
                    case FieldAttributes.PrivateScope:
                        access = Accessibility.Private;
                        break;

                    case FieldAttributes.Public:
                        access = Accessibility.Public;
                        break;

                    case FieldAttributes.Family:
                        access = Accessibility.Protected;
                        break;

                    default:
                        access = Accessibility.Private;
                        break;
                }

                return access;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (_flags & FieldAttributes.Static) != 0;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                var attributes = loadAndFilterAttributes(out var hasRequiredMemberAttribute);
                _packedFlags.SetHasRequiredMemberAttribute(hasRequiredMemberAttribute);
                ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, attributes);
            }
            return _lazyCustomAttributes;

            ImmutableArray<CSharpAttributeData> loadAndFilterAttributes(out bool hasRequiredMemberAttribute)
            {
                hasRequiredMemberAttribute = false;

                var containingModule = ContainingPEModule;
                if (!containingModule.TryGetNonEmptyCustomAttributes(_handle, out var customAttributeHandles))
                {
                    return [];
                }

                var filterOutDecimalConstantAttribute = FilterOutDecimalConstantAttribute();
                using var builder = TemporaryArray<CSharpAttributeData>.Empty;
                foreach (var handle in customAttributeHandles)
                {
                    if (filterOutDecimalConstantAttribute && containingModule.AttributeMatchesFilter(handle, AttributeDescription.DecimalConstantAttribute))
                        continue;

                    if (containingModule.AttributeMatchesFilter(handle, AttributeDescription.RequiredMemberAttribute))
                    {
                        hasRequiredMemberAttribute = true;
                        continue;
                    }

                    builder.Add(new PEAttributeData(containingModule, handle));
                }

                return builder.ToImmutableAndClear();
            }
        }

        private bool FilterOutDecimalConstantAttribute()
        {
            ConstantValue value;
            return this.Type.SpecialType == SpecialType.System_Decimal &&
                   (object)(value = GetConstantValue(ConstantFieldsInProgress.Empty, earlyDecodingWellKnownAttributes: false)) != null &&
                   value.Discriminator == ConstantValueTypeDiscriminator.Decimal;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            foreach (CSharpAttributeData attribute in GetAttributes())
            {
                yield return attribute;
            }

            // Yield hidden attributes last, order might be important.
            if (FilterOutDecimalConstantAttribute())
            {
                var containingPEModuleSymbol = _containingType.ContainingPEModule;
                yield return new PEAttributeData(containingPEModuleSymbol,
                                          containingPEModuleSymbol.Module.FindLastTargetAttribute(_handle, AttributeDescription.DecimalConstantAttribute).Handle);
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref _lazyDocComment);
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            AssemblySymbol primaryDependency = PrimaryDependency;

            if (!_lazyCachedUseSiteInfo.IsInitialized)
            {
                UseSiteInfo<AssemblySymbol> result = new UseSiteInfo<AssemblySymbol>(primaryDependency);
                CalculateUseSiteDiagnostic(ref result);
                if (RefKind != RefKind.None &&
                    (IsFixedSizeBuffer || Type.IsRefLikeOrAllowsRefLikeType()))
                {
                    MergeUseSiteInfo(ref result, new UseSiteInfo<AssemblySymbol>(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this)));
                }
                deriveCompilerFeatureRequiredUseSiteInfo(ref result);
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, result);
            }

            return _lazyCachedUseSiteInfo.ToUseSiteInfo(primaryDependency);

            void deriveCompilerFeatureRequiredUseSiteInfo(ref UseSiteInfo<AssemblySymbol> result)
            {
                var containingType = (PENamedTypeSymbol)ContainingType;
                PEModuleSymbol containingPEModule = _containingType.ContainingPEModule;
                var diag = PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(
                    this,
                    containingPEModule,
                    Handle,
                    allowedFeatures: CompilerFeatureRequiredFeatures.None,
                    new MetadataDecoder(containingPEModule, containingType));

                diag ??= containingType.GetCompilerFeatureRequiredDiagnostic();

                if (diag != null)
                {
                    result = new UseSiteInfo<AssemblySymbol>(diag);
                }
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (PEModuleSymbol)(this.ContainingModule), ignoreByRefLikeMarker: false, ignoreRequiredMemberMarker: false);
                return _lazyObsoleteAttributeData;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool IsRequired
        {
            get
            {
                if (!_packedFlags.TryGetHasRequiredMemberAttribute(out bool hasRequiredMemberAttribute))
                {
                    hasRequiredMemberAttribute = ContainingPEModule.Module.HasAttribute(_handle, AttributeDescription.RequiredMemberAttribute);
                    _packedFlags.SetHasRequiredMemberAttribute(hasRequiredMemberAttribute);
                }

                return hasRequiredMemberAttribute;
            }
        }
    }
}
