// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all methods imported from a PE/module.
    /// </summary>
    internal sealed class PEMethodSymbol : MethodSymbol
    {
        private readonly MethodHandle handle;
        private readonly string name;
        private readonly MethodImplAttributes implFlags;
        private readonly MethodAttributes flags;
        private readonly PENamedTypeSymbol containingType;

        private Symbol associatedPropertyOrEventOpt;

        // The bitSet int is used to compact many different bits of information efficiently into a 32
        // bit int.  The layout is currently:
        //
        // |                     |g|f|e|d|c|b|aaaa|
        // 
        // a = method kind.  4 bits.
        // b = method kind populated.  1 bit.
        //
        // c = isExtensionMethod.  1 bit.
        // d = isExtensionMethod populated.  1 bit.
        //
        // e = isExplicitFinalizerOverride.  1 bit.
        // f = isExplicitClassOverride.  1 bit.
        // g = isExplicitFinalizerOverride and isExplicitClassOverride populated.  1 bit.
        //
        // 23 bits remain for future purposes.
        private int bitSet;

        private const int MethodKindMask = 0xF;
        private const int MethodKindIsPopulatedMask = 1 << 4;
        private const int IsExtensionMethodMask = 1 << 5;
        private const int IsExtensionMethodIsPopulatedMask = 1 << 6;
        private const int IsExplicitFinalizerOverrideMask = 1 << 7;
        private const int IsExplicitClassOverrideMask = 1 << 8;
        private const int IsExplicitOverrideIsPopulatedMask = 1 << 9;

        private ImmutableArray<TypeParameterSymbol> lazyTypeParameters;

        // CONSIDER: Should we use a CustomAttributeBag for PE symbols?
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
        private ImmutableArray<string> lazyConditionalAttributeSymbols;
        private ObsoleteAttributeData lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        private Tuple<CultureInfo, string> lazyDocComment;

        private ImmutableArray<MethodSymbol> lazyExplicitMethodImplementations;
        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state.
        private OverriddenOrHiddenMembersResult lazyOverriddenOrHiddenMembersResult;

        #region "Signature data"

        private SignatureData lazySignature;

        private class SignatureData
        {
            public readonly byte CallingConvention;
            public readonly ImmutableArray<ParameterSymbol> Parameters;
            public readonly PEParameterSymbol ReturnParam;

            public SignatureData(byte callingConvention, ImmutableArray<ParameterSymbol> parameters, PEParameterSymbol returnParam)
            {
                this.CallingConvention = callingConvention;
                this.Parameters = parameters;
                this.ReturnParam = returnParam;
            }
        }

        #endregion

        internal PEMethodSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            MethodHandle methodDef)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!methodDef.IsNil);

            this.handle = methodDef;
            this.containingType = containingType;

            MethodAttributes localflags = 0;

            try
            {
                int rva;
                moduleSymbol.Module.GetMethodDefPropsOrThrow(methodDef, out this.name, out this.implFlags, out localflags, out rva);
            }
            catch (BadImageFormatException)
            {
                if ((object)this.name == null)
                {
                    this.name = String.Empty;
                }

                lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            this.flags = localflags;
        }

        private ParameterSymbol lazyThisParameter;
        internal sealed override ParameterSymbol ThisParameter
        {
            get
            {
                var thisParam = lazyThisParameter;
                if ((object)thisParam != null || IsStatic)
                {
                    return thisParam;
                }

                Interlocked.CompareExchange(ref lazyThisParameter, new ThisParameterSymbol(this), null);
                return lazyThisParameter;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (flags & MethodAttributes.SpecialName) != 0;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (flags & MethodAttributes.RTSpecialName) != 0;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return (System.Reflection.MethodImplAttributes)implFlags;
            }
        }

        internal MethodAttributes Flags
        {
            get
            {
                return flags;
            }
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return (flags & MethodAttributes.RequireSecObject) != 0;
            }
        }

        public override DllImportData GetDllImportData()
        {
            if ((flags & MethodAttributes.PinvokeImpl) == 0)
            {
                return null;
            }

            // do not cache the result, the compiler doesn't use this (it's only exposed thru public API):
            return this.containingType.ContainingPEModule.Module.GetDllImportData(this.handle);
        }

        internal override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return ReturnTypeParameter.IsMarshalledExplicitly;
            }
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return ReturnTypeParameter.MarshallingInformation;
            }
        }

        internal override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return ReturnTypeParameter.MarshallingDescriptor;
            }
        }

        internal override bool IsAccessCheckedOnOverride
        {
            get
            {
                return (flags & MethodAttributes.CheckAccessOnOverride) != 0;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get
            {
                return (flags & MethodAttributes.HasSecurity) != 0;
            }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                var access = Accessibility.Private;

                switch (this.flags & MethodAttributes.MemberAccessMask)
                {
                    case MethodAttributes.Assembly:
                        access = Accessibility.Internal;
                        break;

                    case MethodAttributes.FamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case MethodAttributes.FamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case MethodAttributes.Private:
                    case MethodAttributes.PrivateScope:
                        access = Accessibility.Private;
                        break;

                    case MethodAttributes.Public:
                        access = Accessibility.Public;
                        break;

                    case MethodAttributes.Family:
                        access = Accessibility.Protected;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.flags);
                }

                return access;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return (this.flags & MethodAttributes.PinvokeImpl) != 0;
            }
        }

        internal override bool IsExternal
        {
            get
            {
                return IsExtern || (this.implFlags & MethodImplAttributes.Runtime) != 0;
            }
        }

        public override bool IsVararg
        {
            get
            {
                EnsureSignatureIsLoaded();
                return SignatureHeader.IsVarArgCallSignature(lazySignature.CallingConvention);
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return Arity > 0;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return false;
            }
        }

        public override int Arity
        {
            get
            {
                if (this.lazyTypeParameters.IsDefault)
                {
                    try
                    {
                        int parameterCount;
                        int typeParameterCount;
                        new MetadataDecoder(this.containingType.ContainingPEModule, this).GetSignatureCountsOrThrow(this.handle, out parameterCount, out typeParameterCount);
                        return typeParameterCount;
                    }
                    catch (BadImageFormatException)
                    {
                        return TypeParameters.Length;
                    }
                }
                else
                {
                    return this.lazyTypeParameters.Length;
                }
            }
        }

        internal MethodHandle Handle
        {
            get
            {
                return this.handle;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Has to have the abstract flag.
                // NOTE: dev10 treats the method as abstract (i.e. requiring an impl in subtypes) event if it is not metadata virtual.
                return (this.flags & MethodAttributes.Abstract) != 0;
            }
        }

        public override bool IsSealed
        {
            get
            {
                // NOTE: abstract final methods are a bit strange.  First, they don't
                // PEVerify - there's a specific error message for that combination of modifiers.
                // Second, if dev10 sees an abstract final method in a base class, it will report
                // an error (CS0534) if it is not overridden.  Third, dev10 does not report an
                // error if it is overridden - it emits a virtual method without the newslot
                // modifier as for a normal override.  It is not clear how the runtime rules
                // interpret this overriding method since the overridden method is invalid.
                return this.HasFinalFlag && !this.IsAbstract && this.IsOverride; //slowest check last
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return (this.flags & MethodAttributes.HideBySig) == 0;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // Has to be metadata virtual and cannot be a destructor.  Cannot be either abstract or override.
                // Final is a little special - if a method has the virtual, newslot, and final attr
                // (and is not an explicit override) then we treat it as non-virtual for C# purposes.
                return this.IsMetadataVirtual() && !this.IsDestructor && !this.HasFinalFlag && !this.IsAbstract && !this.IsOverride;
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Has to be metadata virtual and cannot be a destructor.  
                // Must either lack the newslot flag or be an explicit override (i.e. via the MethodImpl table).
                // The IsExplicitClassOverride case is based on LangImporter::DefineMethodImplementations in the native compiler.

                // ECMA-335 
                // 10.3.1 Introducing a virtual method
                // If the definition is not marked newslot, the definition creates a new virtual method only 
                // if there is not virtual method of the same name and signature inherited from a base class.
                //
                // This means that a virtual method without NewSlot flag in a type that doesn't have a base
                // is a new virtual method and doesn't override anything.

                return this.IsMetadataVirtual() && !this.IsDestructor &&
                       ((!this.IsMetadataNewSlot() && (object)this.containingType.BaseTypeNoUseSiteDiagnostics != null) || this.IsExplicitClassOverride);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return (this.flags & MethodAttributes.Static) != 0;
            }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return (this.flags & MethodAttributes.Virtual) != 0;
        }

        internal sealed override bool IsMetadataFinal()
        {
            return HasFinalFlag;
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return (this.flags & MethodAttributes.NewSlot) != 0;
        }

        internal override bool HasFinalFlag
        {
            get
            {
                return (this.flags & MethodAttributes.Final) != 0;
            }
        }

        private bool IsExplicitFinalizerOverride
        {
            get
            {
                if ((this.bitSet & IsExplicitOverrideIsPopulatedMask) == 0)
                {
                    var unused = this.ExplicitInterfaceImplementations;
                    Debug.Assert((this.bitSet & IsExplicitOverrideIsPopulatedMask) != 0);
                }
                return (this.bitSet & IsExplicitFinalizerOverrideMask) != 0;
            }
        }

        private bool IsExplicitClassOverride
        {
            get
            {
                if ((this.bitSet & IsExplicitOverrideIsPopulatedMask) == 0)
                {
                    var unused = this.ExplicitInterfaceImplementations;
                    Debug.Assert((this.bitSet & IsExplicitOverrideIsPopulatedMask) != 0);
                }
                return (this.bitSet & IsExplicitClassOverrideMask) != 0;
            }
        }

        private bool IsDestructor
        {
            get { return this.MethodKind == MethodKind.Destructor; }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return this.ReturnType.SpecialType == SpecialType.System_Void;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                if (this.lazySignature == null)
                {
                    try
                    {
                        int parameterCount;
                        int typeParameterCount;
                        new MetadataDecoder(this.containingType.ContainingPEModule, this).GetSignatureCountsOrThrow(this.handle, out parameterCount, out typeParameterCount);
                        return parameterCount;
                    }
                    catch (BadImageFormatException)
                    {
                        return Parameters.Length;
                    }
                }
                else
                {
                    return this.lazySignature.Parameters.Length;
                }
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazySignature.Parameters;
            }
        }

        internal PEParameterSymbol ReturnTypeParameter
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazySignature.ReturnParam;
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazySignature.ReturnParam.Type;
            }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                EnsureSignatureIsLoaded();
                return lazySignature.ReturnParam.CustomModifiers;
            }
        }

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
            if ((object)this.associatedPropertyOrEventOpt == null)
            {
                Debug.Assert(propertyOrEventSymbol.ContainingType == this.containingType);

                // No locking required since SetAssociatedProperty/SetAssociatedEvent will only be called
                // by the thread that created the method symbol (and will be called before the method
                // symbol is added to the containing type members and available to other threads).
                this.associatedPropertyOrEventOpt = propertyOrEventSymbol;
                Debug.Assert((MethodKindMask & (int)methodKind) == (int)methodKind);
                // NOTE: may be overwriting an existing value.
                Debug.Assert(
                    (this.bitSet & MethodKindMask) == 0 ||
                    (this.bitSet & MethodKindMask) == (int)MethodKind.Ordinary ||
                    (this.bitSet & MethodKindMask) == (int)MethodKind.ExplicitInterfaceImplementation);
                this.bitSet = (this.bitSet & ~MethodKindMask) | (MethodKindMask & (int)methodKind) | MethodKindIsPopulatedMask;
                Debug.Assert((MethodKind)(this.bitSet & MethodKindMask) == methodKind);
                return true;
            }

            return false;
        }

        private void EnsureSignatureIsLoaded()
        {
            if (lazySignature == null)
            {
                LoadSignature();
            }
        }

        private void LoadSignature()
        {
            var moduleSymbol = this.containingType.ContainingPEModule;

            byte callingConvention;
            BadImageFormatException mrEx;
            MetadataDecoder.ParamInfo[] paramInfo = new MetadataDecoder(moduleSymbol, this).GetSignatureForMethod(this.handle, out callingConvention, out mrEx);
            bool makeBad = (mrEx != null);

            // If method is not generic, let's assign empty list for type parameters
            if (!SignatureHeader.IsGeneric(callingConvention) &&
                lazyTypeParameters.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref lazyTypeParameters,
                                        ImmutableArray<TypeParameterSymbol>.Empty, default(ImmutableArray<TypeParameterSymbol>));
            }

            int count = paramInfo.Length - 1;
            ImmutableArray<ParameterSymbol> @params;
            bool isBadParameter;

            if (count > 0)
            {
                ParameterSymbol[] parameterCreation = new ParameterSymbol[count];

                for (int i = 0; i < count; i++)
                {
                    parameterCreation[i] = new PEParameterSymbol(moduleSymbol, this, i, paramInfo[i + 1], out isBadParameter);
                    if (isBadParameter)
                    {
                        makeBad = true;
                    }
                }

                @params = parameterCreation.AsImmutableOrNull();
            }
            else
            {
                @params = ImmutableArray<ParameterSymbol>.Empty;
            }

            // paramInfo[0] contains information about return "parameter"
            Debug.Assert(!paramInfo[0].IsByRef);

            // Dynamify object type if necessary
            paramInfo[0].Type = paramInfo[0].Type.AsDynamicIfNoPia(this.containingType);

            var returnParam = new PEParameterSymbol(moduleSymbol, this, 0, paramInfo[0], out isBadParameter);

            if (makeBad || isBadParameter)
            {
                var old = Interlocked.CompareExchange(ref lazyUseSiteDiagnostic, new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this), CSDiagnosticInfo.EmptyErrorInfo);
                Debug.Assert((object)old == (object)CSDiagnosticInfo.EmptyErrorInfo ||
                             ((object)old != null && old.Code == (int)ErrorCode.ERR_BindToBogus && old.Arguments.Length == 1 && old.Arguments[0] == (object)this));
            }

            var signature = new SignatureData(callingConvention, @params, returnParam);

            Interlocked.CompareExchange(ref lazySignature, signature, null);
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                EnsureTypeParametersAreLoaded();
                return lazyTypeParameters;
            }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                if (IsGenericMethod)
                {
                    return this.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
                }
                else
                {
                    return ImmutableArray<TypeSymbol>.Empty;
                }
            }
        }

        private void EnsureTypeParametersAreLoaded()
        {
            if (lazyTypeParameters.IsDefault)
            {
                ImmutableArray<TypeParameterSymbol> typeParams;

                try
                {
                    var moduleSymbol = this.containingType.ContainingPEModule;
                    var gpHandles = moduleSymbol.Module.GetGenericParametersForMethodOrThrow(this.handle);

                    if (gpHandles.Count == 0)
                    {
                        typeParams = ImmutableArray<TypeParameterSymbol>.Empty;
                    }
                    else
                    {
                        TypeParameterSymbol[] ownedParams = new PETypeParameterSymbol[gpHandles.Count];

                        for (int i = 0; i < ownedParams.Length; i++)
                        {
                            ownedParams[i] = new PETypeParameterSymbol(moduleSymbol, this, (ushort)i, gpHandles[i]);
                        }

                        typeParams = ImmutableArray.Create<TypeParameterSymbol>(ownedParams);
                    }
                }
                catch (BadImageFormatException)
                {
                    var old = Interlocked.CompareExchange(ref lazyUseSiteDiagnostic, new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this), CSDiagnosticInfo.EmptyErrorInfo);
                    Debug.Assert((object)old == (object)CSDiagnosticInfo.EmptyErrorInfo ||
                                    ((object)old != null && old.Code == (int)ErrorCode.ERR_BindToBogus && old.Arguments.Length == 1 && old.Arguments[0] == (object)this));

                    typeParams = ImmutableArray<TypeParameterSymbol>.Empty;
                }

                ImmutableInterlocked.InterlockedCompareExchange(ref lazyTypeParameters, typeParams, default(ImmutableArray<TypeParameterSymbol>));
            }
        }

        public override Symbol AssociatedSymbol
        {
            get { return this.associatedPropertyOrEventOpt; }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                // This is also populated by loading attributes, but
                // loading attributes is more expensive, so we should only do it if
                // attributes are requested.
                if ((this.bitSet & IsExtensionMethodIsPopulatedMask) == 0)
                {
                    int isExtensionMethodBits = 0;
                    if (this.MethodKind == MethodKind.Ordinary && IsValidExtensionMethodSignature()
                        && this.ContainingType.MightContainExtensionMethods)
                    {
                        var moduleSymbol = this.containingType.ContainingPEModule;
                        if (moduleSymbol.Module.HasExtensionAttribute(this.handle, ignoreCase: false))
                        {
                            isExtensionMethodBits = IsExtensionMethodMask;
                        }
                    }
                    ThreadSafeFlagOperations.Set(ref this.bitSet, isExtensionMethodBits | IsExtensionMethodIsPopulatedMask);
                }
                return (this.bitSet & IsExtensionMethodMask) != 0;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return containingType.ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
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
            if (this.lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = this.containingType.ContainingPEModule;

                // Could this possibly be an extension method?
                bool alreadySet = (bitSet & IsExtensionMethodIsPopulatedMask) != 0;
                bool checkForExtension = alreadySet
                    ? (bitSet & IsExtensionMethodMask) != 0
                    : this.MethodKind == MethodKind.Ordinary
                        && IsValidExtensionMethodSignature()
                        && this.containingType.MightContainExtensionMethods;

                bool isExtensionMethod = false;
                if (checkForExtension)
                {
                    containingPEModuleSymbol.LoadCustomAttributesFilterExtensions(this.handle,
                        ref this.lazyCustomAttributes,
                        out isExtensionMethod);
                }
                else
                {
                    containingPEModuleSymbol.LoadCustomAttributes(this.handle,
                        ref this.lazyCustomAttributes);
                }

                if (!alreadySet)
                {
                    ThreadSafeFlagOperations.Set(ref this.bitSet,
                            isExtensionMethod
                            ? IsExtensionMethodMask | IsExtensionMethodIsPopulatedMask
                            : IsExtensionMethodIsPopulatedMask);
                }
            }
            return this.lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return GetAttributes();
        }

        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            EnsureSignatureIsLoaded();
            return lazySignature.ReturnParam.GetAttributes();
        }

        public override MethodKind MethodKind
        {
            get
            {
                if ((this.bitSet & MethodKindIsPopulatedMask) == 0)
                {
                    int methodKindInt = (int)this.ComputeMethodKind();
                    Debug.Assert((MethodKindMask & methodKindInt) == methodKindInt);
                    ThreadSafeFlagOperations.Set(ref this.bitSet, (MethodKindMask & methodKindInt) | MethodKindIsPopulatedMask);
                }
                return (MethodKind)(this.bitSet & MethodKindMask);
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

        private bool IsValidUserDefinedOperatorSignature(int parameterCount)
        {
            return
                !this.ReturnsVoid &&
                !this.IsGenericMethod &&
                !this.IsVararg &&
                this.ParameterCount == parameterCount &&
                this.ParameterRefKinds.IsDefault && // No 'ref' or 'out'
                !this.IsParams();
        }

        private MethodKind ComputeMethodKind()
        {
            if (this.HasSpecialName)
            {
                if (this.name.StartsWith(".", StringComparison.Ordinal))
                {
                    // 10.5.1 Instance constructor
                    // An instance constructor shall be an instance (not static or virtual) method,
                    // it shall be named .ctor, and marked instance, rtspecialname, and specialname (§15.4.2.6).
                    // An instance constructor can have parameters, but shall not return a value.
                    // An instance constructor cannot take generic type parameters.

                    // 10.5.3 Type initializer
                    // This method shall be static, take no parameters, return no value,
                    // be marked with rtspecialname and specialname (§15.4.2.6), and be named .cctor.

                    if ((this.flags & (MethodAttributes.RTSpecialName | MethodAttributes.Virtual)) == MethodAttributes.RTSpecialName &&
                        name.Equals(this.IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName) &&
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
                else if (!this.HasRuntimeSpecialName && this.IsStatic && this.DeclaredAccessibility == Accessibility.Public)
                {
                    switch (this.name)
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
                            // UNDONE: Non-C#-supported overloaded operator case WellKnownMemberNames.ConcatenateOperatorName:
                            // UNDONE: Non-C#-supported overloaded operator case WellKnownMemberNames.ExponentOperatorName:
                            // UNDONE: Non-C#-supported overloaded operator case WellKnownMemberNames.IntegerDivisionOperatorName:
                            // UNDONE: Non-C#-supported overloaded operator case WellKnownMemberNames.LikeOperatorName:
                    }

                    return MethodKind.Ordinary;
                }
            }

            if (!this.IsStatic)
            {
                switch (this.name)
                {
                    case WellKnownMemberNames.DestructorName:
                        if ((this.ContainingType.TypeKind == TypeKind.Class && this.IsRuntimeFinalizer(skipFirstMethodKindCheck: true)) ||
                            this.IsExplicitFinalizerOverride)
                        {
                            return MethodKind.Destructor;
                        }
                        break;
                    case WellKnownMemberNames.DelegateInvokeName:
                        if (this.containingType.TypeKind == TypeKind.Delegate)
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

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                EnsureSignatureIsLoaded();
                return (Microsoft.Cci.CallingConvention)lazySignature.CallingConvention;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (lazyExplicitMethodImplementations.IsDefault)
                {
                    var moduleSymbol = this.containingType.ContainingPEModule;

                    // Context: we need the containing type of this method as context so that we can substitute appropriately into
                    // any generic interfaces that we might be explicitly implementing.  There is no reason to pass in the method
                    // context, however, because any method type parameters will belong to the implemented (i.e. interface) method,
                    // which we do not yet know.
                    var explicitlyOverriddenMethods = new MetadataDecoder(moduleSymbol, this.containingType).GetExplicitlyOverriddenMethods(this.containingType.Handle, this.handle, this.ContainingType);

                    //avoid allocating a builder in the common case
                    var anyToRemove = false;
                    var sawObjectFinalize = false;
                    foreach (var method in explicitlyOverriddenMethods)
                    {
                        if (!method.ContainingType.IsInterface)
                        {
                            anyToRemove = true;
                            sawObjectFinalize = sawObjectFinalize ||
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
                    ThreadSafeFlagOperations.Set(ref this.bitSet,
                        (sawObjectFinalize ? IsExplicitFinalizerOverrideMask : 0) | (anyToRemove ? IsExplicitClassOverrideMask : 0) | IsExplicitOverrideIsPopulatedMask);

                    var explicitInterfaceImplementations = explicitlyOverriddenMethods;

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

                    ImmutableInterlocked.InterlockedCompareExchange(ref this.lazyExplicitMethodImplementations, explicitInterfaceImplementations, default(ImmutableArray<MethodSymbol>));
                }
                return this.lazyExplicitMethodImplementations;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, containingType.ContainingPEModule, preferredCulture, cancellationToken, ref lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if ((object)lazyUseSiteDiagnostic == (object)CSDiagnosticInfo.EmptyErrorInfo)
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                EnsureTypeParametersAreLoaded();
                Interlocked.CompareExchange(ref lazyUseSiteDiagnostic, result, CSDiagnosticInfo.EmptyErrorInfo);
            }

            return lazyUseSiteDiagnostic;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            if (this.lazyConditionalAttributeSymbols.IsDefault)
            {
                var moduleSymbol = this.containingType.ContainingPEModule;
                ImmutableArray<string> conditionalSymbols = moduleSymbol.Module.GetConditionalAttributeValues(this.handle);
                Debug.Assert(!conditionalSymbols.IsDefault);
                ImmutableInterlocked.InterlockedCompareExchange(ref lazyConditionalAttributeSymbols, conditionalSymbols, default(ImmutableArray<string>));
            }

            return this.lazyConditionalAttributeSymbols;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref lazyObsoleteAttributeData, this.handle, (PEModuleSymbol)(this.ContainingModule));
                return lazyObsoleteAttributeData;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal sealed override OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                if ((object)lazyOverriddenOrHiddenMembersResult == null)
                {
                    Interlocked.CompareExchange(ref lazyOverriddenOrHiddenMembersResult, base.OverriddenOrHiddenMembers, null);
                }
                return lazyOverriddenOrHiddenMembersResult;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        // Internal for unit test
        internal bool TestIsExtensionBitSet
        {
            get
            {
                return (this.bitSet & IsExtensionMethodIsPopulatedMask) != 0;
            }
        }

        // Internal for unit test
        internal bool TestIsExtensionBitTrue
        {
            get
            {
                return (this.bitSet & IsExtensionMethodMask) != 0;
            }
        }
    }
}
