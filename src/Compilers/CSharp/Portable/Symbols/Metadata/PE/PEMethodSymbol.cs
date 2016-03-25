// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all methods imported from a PE/module.
    /// </summary>
    internal sealed class PEMethodSymbol : MethodSymbol
    {
        private class SignatureData
        {
            public readonly SignatureHeader Header;
            public readonly ImmutableArray<ParameterSymbol> Parameters;
            public readonly PEParameterSymbol ReturnParam;

            public SignatureData(SignatureHeader header, ImmutableArray<ParameterSymbol> parameters, PEParameterSymbol returnParam)
            {
                this.Header = header;
                this.Parameters = parameters;
                this.ReturnParam = returnParam;
            }
        }

        // This type is used to compact many different bits of information efficiently.
        private struct PackedFlags
        {
            // We currently pack everything into a 32-bit int with the following layout:
            //
            // |            m|l|k|j|i|h|g|f|e|d|c|b|aaaaa|
            // 
            // a = method kind. 5 bits.
            // b = method kind populated. 1 bit.
            //
            // c = isExtensionMethod. 1 bit.
            // d = isExtensionMethod populated. 1 bit.
            //
            // e = isExplicitFinalizerOverride. 1 bit.
            // f = isExplicitClassOverride. 1 bit.
            // g = isExplicitFinalizerOverride and isExplicitClassOverride populated. 1 bit.
            // h = isObsoleteAttribute populated. 1 bit
            // i = isCustomAttributesPopulated. 1 bit
            // j = isUseSiteDiagnostic populated. 1 bit
            // k = isConditional populated. 1 bit
            // l = isOverriddenOrHiddenMembers populated. 1 bit
            // 16 bits remain for future purposes.

            private const int MethodKindOffset = 0;

            private const int MethodKindMask = 0x1F;

            private const int MethodKindIsPopulatedBit = 0x1 << 5;
            private const int IsExtensionMethodBit = 0x1 << 6;
            private const int IsExtensionMethodIsPopulatedBit = 0x1 << 7;
            private const int IsExplicitFinalizerOverrideBit = 0x1 << 8;
            private const int IsExplicitClassOverrideBit = 0x1 << 9;
            private const int IsExplicitOverrideIsPopulatedBit = 0x1 << 10;
            private const int IsObsoleteAttributePopulatedBit = 0x1 << 11;
            private const int IsCustomAttributesPopulatedBit = 0x1 << 12;
            private const int IsUseSiteDiagnosticPopulatedBit = 0x1 << 13;
            private const int IsConditionalPopulatedBit = 0x1 << 14;
            private const int IsOverriddenOrHiddenMembersPopulatedBit = 0x1 << 15;

            private int _bits;

            public MethodKind MethodKind
            {
                get
                {
                    return (MethodKind)((_bits >> MethodKindOffset) & MethodKindMask);
                }

                set
                {
                    Debug.Assert((int)value == ((int)value & MethodKindMask));
                    _bits = (_bits & ~(MethodKindMask << MethodKindOffset)) | (((int)value & MethodKindMask) << MethodKindOffset) | MethodKindIsPopulatedBit;
                }
            }

            public bool MethodKindIsPopulated => (_bits & MethodKindIsPopulatedBit) != 0;
            public bool IsExtensionMethod => (_bits & IsExtensionMethodBit) != 0;
            public bool IsExtensionMethodIsPopulated => (_bits & IsExtensionMethodIsPopulatedBit) != 0;
            public bool IsExplicitFinalizerOverride => (_bits & IsExplicitFinalizerOverrideBit) != 0;
            public bool IsExplicitClassOverride => (_bits & IsExplicitClassOverrideBit) != 0;
            public bool IsExplicitOverrideIsPopulated => (_bits & IsExplicitOverrideIsPopulatedBit) != 0;
            public bool IsObsoleteAttributePopulated => (_bits & IsObsoleteAttributePopulatedBit) != 0;
            public bool IsCustomAttributesPopulated => (_bits & IsCustomAttributesPopulatedBit) != 0;
            public bool IsUseSiteDiagnosticPopulated => (_bits & IsUseSiteDiagnosticPopulatedBit) != 0;
            public bool IsConditionalPopulated => (_bits & IsConditionalPopulatedBit) != 0;
            public bool IsOverriddenOrHiddenMembersPopulated => (_bits & IsOverriddenOrHiddenMembersPopulatedBit) != 0;

#if DEBUG
            static PackedFlags()
            {
                // Verify a few things about the values we combine into flags.  This way, if they ever
                // change, this will get hit and you will know you have to update this type as well.

                // 1) Verify that the range of method kinds doesn't fall outside the bounds of the
                // method kind mask.
                var methodKinds = EnumExtensions.GetValues<MethodKind>();
                var maxMethodKind = (int)System.Linq.Enumerable.Aggregate(methodKinds, (m1, m2) => m1 | m2);
                Debug.Assert((maxMethodKind & MethodKindMask) == maxMethodKind);
            }
#endif

            private static bool BitsAreUnsetOrSame(int bits, int mask)
            {
                return (bits & mask) == 0 || (bits & mask) == mask;
            }

            public void InitializeIsExtensionMethod(bool isExtensionMethod)
            {
                int bitsToSet = (isExtensionMethod ? IsExtensionMethodBit : 0) | IsExtensionMethodIsPopulatedBit;
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet));
                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public void InitializeMethodKind(MethodKind methodKind)
            {
                Debug.Assert((int)methodKind == ((int)methodKind & MethodKindMask));
                int bitsToSet = (((int)methodKind & MethodKindMask) << MethodKindOffset) | MethodKindIsPopulatedBit;
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet));
                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public void InitializeIsExplicitOverride(bool isExplicitFinalizerOverride, bool isExplicitClassOverride)
            {
                int bitsToSet =
                    (isExplicitFinalizerOverride ? IsExplicitFinalizerOverrideBit : 0) |
                    (isExplicitClassOverride ? IsExplicitClassOverrideBit : 0) |
                    IsExplicitOverrideIsPopulatedBit;
                Debug.Assert(BitsAreUnsetOrSame(_bits, bitsToSet));
                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public void SetIsObsoleteAttributePopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsObsoleteAttributePopulatedBit);
            }

            public void SetIsCustomAttributesPopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsCustomAttributesPopulatedBit);
            }

            public void SetIsUseSiteDiagnosticPopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsUseSiteDiagnosticPopulatedBit);
            }

            public void SetIsConditionalAttributePopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsConditionalPopulatedBit);
            }

            public void SetIsOverriddenOrHiddenMembersPopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsOverriddenOrHiddenMembersPopulatedBit);
            }
        }

        /// <summary>
        /// Holds infrequently accessed fields. See <seealso cref="_uncommonFields"/> for an explanation.
        /// </summary>
        private sealed class UncommonFields
        {
            public ParameterSymbol _lazyThisParameter;
            public Tuple<CultureInfo, string> _lazyDocComment;
            public OverriddenOrHiddenMembersResult _lazyOverriddenOrHiddenMembersResult;
            public ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
            public ImmutableArray<string> _lazyConditionalAttributeSymbols;
            public ObsoleteAttributeData _lazyObsoleteAttributeData;
            public DiagnosticInfo _lazyUseSiteDiagnostic;
        }

        private UncommonFields CreateUncommonFields()
        {
            var retVal = new UncommonFields();
            if (!_packedFlags.IsObsoleteAttributePopulated)
            {
                retVal._lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
            }

            if (!_packedFlags.IsUseSiteDiagnosticPopulated)
            {
                retVal._lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state.
            }

            if (_packedFlags.IsCustomAttributesPopulated)
            {
                retVal._lazyCustomAttributes = ImmutableArray<CSharpAttributeData>.Empty;
            }

            if (_packedFlags.IsConditionalPopulated)
            {
                retVal._lazyConditionalAttributeSymbols = ImmutableArray<string>.Empty;
            }

            if (_packedFlags.IsOverriddenOrHiddenMembersPopulated)
            {
                retVal._lazyOverriddenOrHiddenMembersResult = OverriddenOrHiddenMembersResult.Empty;
            }

            return retVal;
        }

        private UncommonFields AccessUncommonFields()
        {
            var retVal = _uncommonFields;
            return retVal ?? InterlockedOperations.Initialize(ref _uncommonFields, CreateUncommonFields());
        }

        private readonly MethodDefinitionHandle _handle;
        private readonly string _name;
        private readonly PENamedTypeSymbol _containingType;
        private Symbol _associatedPropertyOrEventOpt;
        private PackedFlags _packedFlags;
        private readonly ushort _flags;     // MethodAttributes
        private readonly ushort _implFlags; // MethodImplAttributes
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;
        private SignatureData _lazySignature;
        private ImmutableArray<MethodSymbol> _lazyExplicitMethodImplementations;

        /// <summary>
        /// A single field to hold optional auxiliary data.
        /// In many scenarios it is possible to avoid allocating this, thus saving total space in <see cref="PEModuleSymbol"/>.
        /// Even for lazily-computed values, it may be possible to avoid allocating <see cref="_uncommonFields"/> if
        /// the computed value is a well-known "empty" value. In this case, bits in <see cref="_packedFlags"/> are used
        /// to indicate that the lazy values have been computed and, if <see cref="_uncommonFields"/> is null, then
        /// the "empty" value should be inferred.
        /// </summary>
        private UncommonFields _uncommonFields;

        internal PEMethodSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            MethodDefinitionHandle methodDef)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!methodDef.IsNil);

            _handle = methodDef;
            _containingType = containingType;

            MethodAttributes localflags = 0;

            try
            {
                int rva;
                MethodImplAttributes implFlags;
                moduleSymbol.Module.GetMethodDefPropsOrThrow(methodDef, out _name, out implFlags, out localflags, out rva);
                Debug.Assert((uint)implFlags <= ushort.MaxValue);
                _implFlags = (ushort)implFlags;
            }
            catch (BadImageFormatException)
            {
                if ((object)_name == null)
                {
                    _name = string.Empty;
                }

                InitializeUseSiteDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));
            }

            Debug.Assert((uint)localflags <= ushort.MaxValue);
            _flags = (ushort)localflags;
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            thisParameter = IsStatic ? null :
                           _uncommonFields?._lazyThisParameter ?? InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyThisParameter, new ThisParameterSymbol(this));
            return true;
        }

        public override Symbol ContainingSymbol => _containingType;

        public override NamedTypeSymbol ContainingType => _containingType;

        public override string Name => _name;

        private bool HasFlag(MethodAttributes flag)
        {
            // flag must be exactly one bit
            Debug.Assert(flag != 0 && ((ushort)flag & ((ushort)flag - 1)) == 0);
            return ((ushort)flag & _flags) != 0;
        }

        // Exposed for testing purposes only
        internal MethodAttributes Flags => (MethodAttributes)_flags;

        internal override bool HasSpecialName => HasFlag(MethodAttributes.SpecialName);

        internal override bool HasRuntimeSpecialName => HasFlag(MethodAttributes.RTSpecialName);

        internal override MethodImplAttributes ImplementationAttributes => (MethodImplAttributes)_implFlags;

        internal override bool RequiresSecurityObject => HasFlag(MethodAttributes.RequireSecObject);

        // do not cache the result, the compiler doesn't use this (it's only exposed through public API):
        public override DllImportData GetDllImportData() => HasFlag(MethodAttributes.PinvokeImpl)
            ? _containingType.ContainingPEModule.Module.GetDllImportData(_handle)
            : null;

        internal override bool ReturnValueIsMarshalledExplicitly => ReturnTypeParameter.IsMarshalledExplicitly;

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => ReturnTypeParameter.MarshallingInformation;

        internal override ImmutableArray<byte> ReturnValueMarshallingDescriptor => ReturnTypeParameter.MarshallingDescriptor;

        internal override bool IsAccessCheckedOnOverride => HasFlag(MethodAttributes.CheckAccessOnOverride);

        internal override bool HasDeclarativeSecurity => HasFlag(MethodAttributes.HasSecurity);

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                switch (Flags & MethodAttributes.MemberAccessMask)
                {
                    case MethodAttributes.Assembly:
                        return Accessibility.Internal;

                    case MethodAttributes.FamORAssem:
                        return Accessibility.ProtectedOrInternal;

                    case MethodAttributes.FamANDAssem:
                        return Accessibility.ProtectedAndInternal;

                    case MethodAttributes.Private:
                    case MethodAttributes.PrivateScope:
                        return Accessibility.Private;

                    case MethodAttributes.Public:
                        return Accessibility.Public;

                    case MethodAttributes.Family:
                        return Accessibility.Protected;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_flags);
                }
            }
        }

        public override bool IsExtern => HasFlag(MethodAttributes.PinvokeImpl);

        internal override bool IsExternal => IsExtern || (ImplementationAttributes & MethodImplAttributes.Runtime) != 0;

        public override bool IsVararg => Signature.Header.CallingConvention == SignatureCallingConvention.VarArgs;

        public override bool IsGenericMethod => Arity > 0;

        public override bool IsAsync => false;

        public override int Arity
        {
            get
            {
                if (!_lazyTypeParameters.IsDefault)
                {
                    return _lazyTypeParameters.Length;
                }

                try
                {
                    int parameterCount;
                    int typeParameterCount;
                    MetadataDecoder.GetSignatureCountsOrThrow(_containingType.ContainingPEModule.Module, _handle, out parameterCount, out typeParameterCount);
                    return typeParameterCount;
                }
                catch (BadImageFormatException)
                {
                    return TypeParameters.Length;
                }
            }
        }

        internal MethodDefinitionHandle Handle => _handle;

        // Has to have the abstract flag.
        // NOTE: dev10 treats the method as abstract (i.e. requiring an impl in subtypes) event if it is not metadata virtual.
        public override bool IsAbstract => HasFlag(MethodAttributes.Abstract);

        // NOTE: abstract final methods are a bit strange.  First, they don't
        // PEVerify - there's a specific error message for that combination of modifiers.
        // Second, if dev10 sees an abstract final method in a base class, it will report
        // an error (CS0534) if it is not overridden.  Third, dev10 does not report an
        // error if it is overridden - it emits a virtual method without the newslot
        // modifier as for a normal override.  It is not clear how the runtime rules
        // interpret this overriding method since the overridden method is invalid.
        public override bool IsSealed => this.IsMetadataFinal && !this.IsAbstract && this.IsOverride; //slowest check last

        public override bool HidesBaseMethodsByName => !HasFlag(MethodAttributes.HideBySig);

        // Has to be metadata virtual and cannot be a destructor.  Cannot be either abstract or override.
        // Final is a little special - if a method has the virtual, newslot, and final attr
        // (and is not an explicit override) then we treat it as non-virtual for C# purposes.
        public override bool IsVirtual => this.IsMetadataVirtual() && !this.IsDestructor && !this.IsMetadataFinal && !this.IsAbstract && !this.IsOverride;

        // Has to be metadata virtual and cannot be a destructor.  
        // Must either lack the newslot flag or be an explicit override (i.e. via the MethodImpl table).
        //
        // The IsExplicitClassOverride case is based on LangImporter::DefineMethodImplementations in the native compiler.
        // ECMA-335 
        // 10.3.1 Introducing a virtual method
        // If the definition is not marked newslot, the definition creates a new virtual method only 
        // if there is not virtual method of the same name and signature inherited from a base class.
        //
        // This means that a virtual method without NewSlot flag in a type that doesn't have a base
        // is a new virtual method and doesn't override anything.
        public override bool IsOverride =>
            this.IsMetadataVirtual() && !this.IsDestructor &&
                       ((!this.IsMetadataNewSlot() && (object)_containingType.BaseTypeNoUseSiteDiagnostics != null) || this.IsExplicitClassOverride);

        public override bool IsStatic => HasFlag(MethodAttributes.Static);

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => HasFlag(MethodAttributes.Virtual);

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => HasFlag(MethodAttributes.NewSlot);

        internal override bool IsMetadataFinal => HasFlag(MethodAttributes.Final);

        private bool IsExplicitFinalizerOverride
        {
            get
            {
                if (!_packedFlags.IsExplicitOverrideIsPopulated)
                {
                    var unused = this.ExplicitInterfaceImplementations;
                    Debug.Assert(_packedFlags.IsExplicitOverrideIsPopulated);
                }
                return _packedFlags.IsExplicitFinalizerOverride;
            }
        }

        private bool IsExplicitClassOverride
        {
            get
            {
                if (!_packedFlags.IsExplicitOverrideIsPopulated)
                {
                    var unused = this.ExplicitInterfaceImplementations;
                    Debug.Assert(_packedFlags.IsExplicitOverrideIsPopulated);
                }
                return _packedFlags.IsExplicitClassOverride;
            }
        }

        private bool IsDestructor => this.MethodKind == MethodKind.Destructor;

        public override bool ReturnsVoid => this.ReturnType.SpecialType == SpecialType.System_Void;

        internal override int ParameterCount
        {
            get
            {
                if (_lazySignature != null)
                {
                    return _lazySignature.Parameters.Length;
                }

                try
                {
                    int parameterCount;
                    int typeParameterCount;
                    MetadataDecoder.GetSignatureCountsOrThrow(_containingType.ContainingPEModule.Module, _handle,
                        out parameterCount, out typeParameterCount);
                    return parameterCount;
                }
                catch (BadImageFormatException)
                {
                    return Parameters.Length;
                }
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters => Signature.Parameters;

        internal PEParameterSymbol ReturnTypeParameter => Signature.ReturnParam;

        internal override RefKind RefKind => Signature.ReturnParam.RefKind;

        public override TypeSymbol ReturnType => Signature.ReturnParam.Type;

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => Signature.ReturnParam.CustomModifiers;

        /// <summary>
        /// Associate the method with a particular property. Returns
        /// false if the method is already associated with a property or event.
        /// </summary>
        internal bool SetAssociatedProperty(PEPropertySymbol propertySymbol, MethodKind methodKind)
        {
            Debug.Assert((methodKind == MethodKind.PropertyGet) || (methodKind == MethodKind.PropertySet));
            return this.SetAssociatedPropertyOrEvent(propertySymbol, methodKind);
        }

        /// <summary>
        /// Associate the method with a particular event. Returns
        /// false if the method is already associated with a property or event.
        /// </summary>
        internal bool SetAssociatedEvent(PEEventSymbol eventSymbol, MethodKind methodKind)
        {
            Debug.Assert((methodKind == MethodKind.EventAdd) || (methodKind == MethodKind.EventRemove));
            return this.SetAssociatedPropertyOrEvent(eventSymbol, methodKind);
        }

        private bool SetAssociatedPropertyOrEvent(Symbol propertyOrEventSymbol, MethodKind methodKind)
        {
            if ((object)_associatedPropertyOrEventOpt == null)
            {
                Debug.Assert(propertyOrEventSymbol.ContainingType == _containingType);

                // No locking required since SetAssociatedProperty/SetAssociatedEvent will only be called
                // by the thread that created the method symbol (and will be called before the method
                // symbol is added to the containing type members and available to other threads).
                _associatedPropertyOrEventOpt = propertyOrEventSymbol;

                // NOTE: may be overwriting an existing value.
                Debug.Assert(
                    _packedFlags.MethodKind == default(MethodKind) ||
                    _packedFlags.MethodKind == MethodKind.Ordinary ||
                    _packedFlags.MethodKind == MethodKind.ExplicitInterfaceImplementation);

                _packedFlags.MethodKind = methodKind;
                return true;
            }

            return false;
        }

        private SignatureData Signature => _lazySignature ?? LoadSignature();

        private SignatureData LoadSignature()
        {
            var moduleSymbol = _containingType.ContainingPEModule;

            SignatureHeader signatureHeader;
            BadImageFormatException mrEx;
            ParamInfo<TypeSymbol>[] paramInfo = new MetadataDecoder(moduleSymbol, this).GetSignatureForMethod(_handle, out signatureHeader, out mrEx, allowByRefReturn: true);
            bool makeBad = (mrEx != null);

            // If method is not generic, let's assign empty list for type parameters
            if (!signatureHeader.IsGeneric &&
                _lazyTypeParameters.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters,
                    ImmutableArray<TypeParameterSymbol>.Empty);
            }

            int count = paramInfo.Length - 1;
            ImmutableArray<ParameterSymbol> @params;
            bool isBadParameter;

            if (count > 0)
            {
                var builder = ImmutableArray.CreateBuilder<ParameterSymbol>(count);
                for (int i = 0; i < count; i++)
                {
                    builder.Add(PEParameterSymbol.Create(moduleSymbol, this, i, paramInfo[i + 1], out isBadParameter));
                    if (isBadParameter)
                    {
                        makeBad = true;
                    }
                }

                @params = builder.ToImmutable();
            }
            else
            {
                @params = ImmutableArray<ParameterSymbol>.Empty;
            }

            // Dynamify object type if necessary
            paramInfo[0].Type = paramInfo[0].Type.AsDynamicIfNoPia(_containingType);

            var returnParam = PEParameterSymbol.Create(moduleSymbol, this, 0, paramInfo[0], out isBadParameter);

            if (makeBad || isBadParameter)
            {
                InitializeUseSiteDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));
            }

            var signature = new SignatureData(signatureHeader, @params, returnParam);

            return InterlockedOperations.Initialize(ref _lazySignature, signature);
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                DiagnosticInfo diagnosticInfo = null;
                var typeParams = EnsureTypeParametersAreLoaded(ref diagnosticInfo);
                if (diagnosticInfo != null)
                {
                    InitializeUseSiteDiagnostic(diagnosticInfo);
                }

                return typeParams;
            }
        }

        private ImmutableArray<TypeParameterSymbol> EnsureTypeParametersAreLoaded(ref DiagnosticInfo diagnosticInfo)
        {
            var typeParams = _lazyTypeParameters;
            if (!typeParams.IsDefault)
            {
                return typeParams;
            }

            return InterlockedOperations.Initialize(ref _lazyTypeParameters, LoadTypeParameters(ref diagnosticInfo));
        }

        private ImmutableArray<TypeParameterSymbol> LoadTypeParameters(ref DiagnosticInfo diagnosticInfo)
        {
            try
            {
                var moduleSymbol = _containingType.ContainingPEModule;
                var gpHandles = moduleSymbol.Module.GetGenericParametersForMethodOrThrow(_handle);

                if (gpHandles.Count == 0)
                {
                    return ImmutableArray<TypeParameterSymbol>.Empty;
                }
                else
                {
                    var ownedParams = ImmutableArray.CreateBuilder<TypeParameterSymbol>(gpHandles.Count);
                    for (int i = 0; i < gpHandles.Count; i++)
                    {
                        ownedParams.Add(new PETypeParameterSymbol(moduleSymbol, this, (ushort)i, gpHandles[i]));
                    }

                    return ownedParams.ToImmutable();
                }
            }
            catch (BadImageFormatException)
            {
                diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments => IsGenericMethod ? TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>() : ImmutableArray<TypeSymbol>.Empty;

        public override Symbol AssociatedSymbol => _associatedPropertyOrEventOpt;

        public override bool IsExtensionMethod
        {
            get
            {
                // This is also populated by loading attributes, but
                // loading attributes is more expensive, so we should only do it if
                // attributes are requested.
                if (!_packedFlags.IsExtensionMethodIsPopulated)
                {
                    bool isExtensionMethod = false;
                    if (this.MethodKind == MethodKind.Ordinary && IsValidExtensionMethodSignature()
                        && this.ContainingType.MightContainExtensionMethods)
                    {
                        var moduleSymbol = _containingType.ContainingPEModule;
                        isExtensionMethod = moduleSymbol.Module.HasExtensionAttribute(_handle, ignoreCase: false);
                    }
                    _packedFlags.InitializeIsExtensionMethod(isExtensionMethod);
                }
                return _packedFlags.IsExtensionMethod;
            }
        }

        public override ImmutableArray<Location> Locations => _containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (!_packedFlags.IsCustomAttributesPopulated)
            {
                // Compute the value
                var attributeData = default(ImmutableArray<CSharpAttributeData>);
                var containingPEModuleSymbol = _containingType.ContainingPEModule;

                // Could this possibly be an extension method?
                bool alreadySet = _packedFlags.IsExtensionMethodIsPopulated;
                bool checkForExtension = alreadySet
                    ? _packedFlags.IsExtensionMethod
                    : this.MethodKind == MethodKind.Ordinary
                        && IsValidExtensionMethodSignature()
                        && _containingType.MightContainExtensionMethods;

                bool isExtensionMethod = false;
                if (checkForExtension)
                {
                    containingPEModuleSymbol.LoadCustomAttributesFilterExtensions(_handle,
                        ref attributeData,
                        out isExtensionMethod);
                }
                else
                {
                    containingPEModuleSymbol.LoadCustomAttributes(_handle,
                        ref attributeData);
                }

                if (!alreadySet)
                {
                    _packedFlags.InitializeIsExtensionMethod(isExtensionMethod);
                }

                // Store the result in uncommon fields only if it's not empty.
                Debug.Assert(!attributeData.IsDefault);
                if (!attributeData.IsEmpty)
                {
                    attributeData = InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyCustomAttributes, attributeData);
                }

                _packedFlags.SetIsCustomAttributesPopulated();
                return attributeData;
            }

            // Retrieve cached or inferred value.
            var uncommonFields = _uncommonFields;
            if (uncommonFields == null)
            {
                return ImmutableArray<CSharpAttributeData>.Empty;
            }
            else
            {
                var attributeData = uncommonFields._lazyCustomAttributes;
                return attributeData.IsDefault
                    ? InterlockedOperations.Initialize(ref uncommonFields._lazyCustomAttributes, ImmutableArray<CSharpAttributeData>.Empty)
                    : attributeData;
            }
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState) => GetAttributes();

        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes() => Signature.ReturnParam.GetAttributes();

        public override MethodKind MethodKind
        {
            get
            {
                if (!_packedFlags.MethodKindIsPopulated)
                {
                    _packedFlags.InitializeMethodKind(this.ComputeMethodKind());
                }
                return _packedFlags.MethodKind;
            }
        }

        private bool IsValidExtensionMethodSignature()
        {
            if (!this.IsStatic)
            {
                return false;
            }

            var parameters = this.Parameters;
            if (parameters.Length == 0)
            {
                return false;
            }

            var parameter = parameters[0];
            return (parameter.RefKind == RefKind.None) && !parameter.IsParams;
        }

        private bool IsValidUserDefinedOperatorSignature(int parameterCount) =>
                !this.ReturnsVoid &&
                !this.IsGenericMethod &&
                !this.IsVararg &&
                this.ParameterCount == parameterCount &&
                this.ParameterRefKinds.IsDefault && // No 'ref' or 'out'
                !this.IsParams();

        private bool IsValidUserDefinedOperatorIs()
        {
            foreach (var parameter in this.Parameters)
            {
                if (parameter.RefKind != ((parameter.Ordinal == 0) ? RefKind.None : RefKind.Out))
                {
                    return false;
                }
            }

            return
                (this.ReturnsVoid || this.ReturnType.SpecialType != SpecialType.System_Boolean) &&
                !this.IsGenericMethod &&
                !this.IsVararg &&
                this.ParameterCount > 0 &&
                !this.IsParams();
        }

        private MethodKind ComputeMethodKind()
        {
            if (this.HasSpecialName)
            {
                if (_name.StartsWith(".", StringComparison.Ordinal))
                {
                    // 10.5.1 Instance constructor
                    // An instance constructor shall be an instance (not static or virtual) method,
                    // it shall be named .ctor, and marked instance, rtspecialname, and specialname (§15.4.2.6).
                    // An instance constructor can have parameters, but shall not return a value.
                    // An instance constructor cannot take generic type parameters.

                    // 10.5.3 Type initializer
                    // This method shall be static, take no parameters, return no value,
                    // be marked with rtspecialname and specialname (§15.4.2.6), and be named .cctor.

                    if ((Flags & (MethodAttributes.RTSpecialName | MethodAttributes.Virtual)) == MethodAttributes.RTSpecialName &&
                        _name.Equals(this.IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName) &&
                        this.ReturnsVoid && this.Arity == 0)
                    {
                        if (this.IsStatic)
                        {
                            if (Parameters.Length == 0)
                            {
                                return MethodKind.StaticConstructor;
                            }
                        }
                        else
                        {
                            return MethodKind.Constructor;
                        }
                    }

                    return MethodKind.Ordinary;
                }

                if (!this.HasRuntimeSpecialName && this.IsStatic && this.DeclaredAccessibility == Accessibility.Public)
                {
                    switch (_name)
                    {
                        case WellKnownMemberNames.AdditionOperatorName:
                        case WellKnownMemberNames.BitwiseAndOperatorName:
                        case WellKnownMemberNames.BitwiseOrOperatorName:
                        case WellKnownMemberNames.DivisionOperatorName:
                        case WellKnownMemberNames.EqualityOperatorName:
                        case WellKnownMemberNames.ExclusiveOrOperatorName:
                        case WellKnownMemberNames.GreaterThanOperatorName:
                        case WellKnownMemberNames.GreaterThanOrEqualOperatorName:
                        case WellKnownMemberNames.InequalityOperatorName:
                        case WellKnownMemberNames.LeftShiftOperatorName:
                        case WellKnownMemberNames.LessThanOperatorName:
                        case WellKnownMemberNames.LessThanOrEqualOperatorName:
                        case WellKnownMemberNames.ModulusOperatorName:
                        case WellKnownMemberNames.MultiplyOperatorName:
                        case WellKnownMemberNames.RightShiftOperatorName:
                        case WellKnownMemberNames.SubtractionOperatorName:
                            return IsValidUserDefinedOperatorSignature(2) ? MethodKind.UserDefinedOperator : MethodKind.Ordinary;
                        case WellKnownMemberNames.DecrementOperatorName:
                        case WellKnownMemberNames.FalseOperatorName:
                        case WellKnownMemberNames.IncrementOperatorName:
                        case WellKnownMemberNames.LogicalNotOperatorName:
                        case WellKnownMemberNames.OnesComplementOperatorName:
                        case WellKnownMemberNames.TrueOperatorName:
                        case WellKnownMemberNames.UnaryNegationOperatorName:
                        case WellKnownMemberNames.UnaryPlusOperatorName:
                            return IsValidUserDefinedOperatorSignature(1) ? MethodKind.UserDefinedOperator : MethodKind.Ordinary;
                        case WellKnownMemberNames.ImplicitConversionName:
                        case WellKnownMemberNames.ExplicitConversionName:
                            return IsValidUserDefinedOperatorSignature(1) ? MethodKind.Conversion : MethodKind.Ordinary;
                        case WellKnownMemberNames.IsOperatorName:
                            return IsValidUserDefinedOperatorIs() ? MethodKind.UserDefinedOperator : MethodKind.Ordinary;
                        case WellKnownMemberNames.ConcatenateOperatorName:
                        case WellKnownMemberNames.ExponentOperatorName:
                        case WellKnownMemberNames.IntegerDivisionOperatorName:
                        case WellKnownMemberNames.LikeOperatorName:
                            // Non-C#-supported overloaded operator
                            return MethodKind.Ordinary;
                    }

                    return MethodKind.Ordinary;
                }
            }

            if (!this.IsStatic)
            {
                switch (_name)
                {
                    case WellKnownMemberNames.DestructorName:
                        if ((this.ContainingType.TypeKind == TypeKind.Class && this.IsRuntimeFinalizer(skipFirstMethodKindCheck: true)) ||
                            this.IsExplicitFinalizerOverride)
                        {
                            return MethodKind.Destructor;
                        }
                        break;
                    case WellKnownMemberNames.DelegateInvokeName:
                        if (_containingType.TypeKind == TypeKind.Delegate)
                        {
                            return MethodKind.DelegateInvoke;
                        }
                        break;
                    default:
                        // Note: this is expensive, so check it last
                        // Note: method being processed may have an explicit method .override but still be 
                        //       publicly accessible, the decision here is being made based on the method's name
                        if (!SyntaxFacts.IsValidIdentifier(this.Name) && !this.ExplicitInterfaceImplementations.IsEmpty)
                        {
                            return MethodKind.ExplicitInterfaceImplementation;
                        }
                        break;
                }
            }

            return MethodKind.Ordinary;
        }

        internal override Cci.CallingConvention CallingConvention => (Cci.CallingConvention)Signature.Header.RawValue;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                var explicitInterfaceImplementations = _lazyExplicitMethodImplementations;
                if (!explicitInterfaceImplementations.IsDefault)
                {
                    return explicitInterfaceImplementations;
                }

                var moduleSymbol = _containingType.ContainingPEModule;

                // Context: we need the containing type of this method as context so that we can substitute appropriately into
                // any generic interfaces that we might be explicitly implementing.  There is no reason to pass in the method
                // context, however, because any method type parameters will belong to the implemented (i.e. interface) method,
                // which we do not yet know.
                var explicitlyOverriddenMethods = new MetadataDecoder(moduleSymbol, _containingType).GetExplicitlyOverriddenMethods(_containingType.Handle, _handle, this.ContainingType);

                //avoid allocating a builder in the common case
                var anyToRemove = false;
                var sawObjectFinalize = false;
                foreach (var method in explicitlyOverriddenMethods)
                {
                    if (!method.ContainingType.IsInterface)
                    {
                        anyToRemove = true;
                        sawObjectFinalize =
                            (method.ContainingType.SpecialType == SpecialType.System_Object &&
                             method.Name == WellKnownMemberNames.DestructorName && // Cheaper than MethodKind.
                             method.MethodKind == MethodKind.Destructor);
                    }

                    if (anyToRemove && sawObjectFinalize)
                    {
                        break;
                    }
                }

                // CONSIDER: could assert that we're writing the existing value if it's already there
                // CONSIDER: what we'd really like to do is set this bit only in cases where the explicitly
                // overridden method matches the method that will be returned by MethodSymbol.OverriddenMethod.
                // Unfortunately, this MethodSymbol will not be sufficiently constructed (need IsOverride and MethodKind,
                // which depend on this property) to determine which method OverriddenMethod will return.
                _packedFlags.InitializeIsExplicitOverride(isExplicitFinalizerOverride: sawObjectFinalize, isExplicitClassOverride: anyToRemove);

                explicitInterfaceImplementations = explicitlyOverriddenMethods;

                if (anyToRemove)
                {
                    var explicitInterfaceImplementationsBuilder = ArrayBuilder<MethodSymbol>.GetInstance();
                    foreach (var method in explicitlyOverriddenMethods)
                    {
                        if (method.ContainingType.IsInterface)
                        {
                            explicitInterfaceImplementationsBuilder.Add(method);
                        }
                    }

                    explicitInterfaceImplementations = explicitInterfaceImplementationsBuilder.ToImmutableAndFree();
                }

                return InterlockedOperations.Initialize(ref _lazyExplicitMethodImplementations, explicitInterfaceImplementations);
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref AccessUncommonFields()._lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (!_packedFlags.IsUseSiteDiagnosticPopulated)
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                EnsureTypeParametersAreLoaded(ref result);
                return InitializeUseSiteDiagnostic(result);
            }

            var uncommonFields = _uncommonFields;
            if (uncommonFields == null)
            {
                return null;
            }
            else
            {
                var result = uncommonFields._lazyUseSiteDiagnostic;
                return CSDiagnosticInfo.IsEmpty(result)
                       ? InterlockedOperations.Initialize(ref uncommonFields._lazyUseSiteDiagnostic, null, CSDiagnosticInfo.EmptyErrorInfo)
                       : result;
            }
        }

        private DiagnosticInfo InitializeUseSiteDiagnostic(DiagnosticInfo diagnostic)
        {
            Debug.Assert(!CSDiagnosticInfo.IsEmpty(diagnostic));
            if (diagnostic != null)
            {
                diagnostic = InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyUseSiteDiagnostic, diagnostic, CSDiagnosticInfo.EmptyErrorInfo);
            }

            _packedFlags.SetIsUseSiteDiagnosticPopulated();
            return diagnostic;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            if (!_packedFlags.IsConditionalPopulated)
            {
                var result = _containingType.ContainingPEModule.Module.GetConditionalAttributeValues(_handle);
                Debug.Assert(!result.IsDefault);
                if (!result.IsEmpty)
                {
                    result = InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyConditionalAttributeSymbols, result);
                }

                _packedFlags.SetIsConditionalAttributePopulated();
                return result;
            }

            var uncommonFields = _uncommonFields;
            if (uncommonFields == null)
            {
                return ImmutableArray<string>.Empty;
            }
            else
            {
                var result = uncommonFields._lazyConditionalAttributeSymbols;
                return result.IsDefault
                    ? InterlockedOperations.Initialize(ref uncommonFields._lazyConditionalAttributeSymbols, ImmutableArray<string>.Empty)
                    : result;
            }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                if (!_packedFlags.IsObsoleteAttributePopulated)
                {
                    var result = ObsoleteAttributeHelpers.GetObsoleteDataFromMetadata(_handle, (PEModuleSymbol)ContainingModule);
                    if (result != null)
                    {
                        result = InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyObsoleteAttributeData, result, ObsoleteAttributeData.Uninitialized);
                    }

                    _packedFlags.SetIsObsoleteAttributePopulated();
                    return result;
                }

                var uncommonFields = _uncommonFields;
                if (uncommonFields == null)
                {
                    return null;
                }
                else
                {
                    var result = uncommonFields._lazyObsoleteAttributeData;
                    return ReferenceEquals(result, ObsoleteAttributeData.Uninitialized)
                        ? InterlockedOperations.Initialize(ref uncommonFields._lazyObsoleteAttributeData, null, ObsoleteAttributeData.Uninitialized)
                        : result;
                }
            }
        }

        internal override bool GenerateDebugInfo => false;

        internal override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                if (!_packedFlags.IsOverriddenOrHiddenMembersPopulated)
                {
                    var result = base.OverriddenOrHiddenMembers;
                    Debug.Assert(result != null);
                    if (result != OverriddenOrHiddenMembersResult.Empty)
                    {
                        result = InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyOverriddenOrHiddenMembersResult, result);
                    }

                    _packedFlags.SetIsOverriddenOrHiddenMembersPopulated();
                    return result;
                }

                var uncommonFields = _uncommonFields;
                if (uncommonFields == null)
                {
                    return OverriddenOrHiddenMembersResult.Empty;
                }

                return uncommonFields._lazyOverriddenOrHiddenMembersResult ?? InterlockedOperations.Initialize(ref uncommonFields._lazyOverriddenOrHiddenMembersResult, OverriddenOrHiddenMembersResult.Empty);
            }
        }

        // perf, not correctness
        internal override CSharpCompilation DeclaringCompilation => null;

        // Internal for unit test
        internal bool TestIsExtensionBitSet => _packedFlags.IsExtensionMethodIsPopulated;

        // Internal for unit test
        internal bool TestIsExtensionBitTrue => _packedFlags.IsExtensionMethod;
    }
}
