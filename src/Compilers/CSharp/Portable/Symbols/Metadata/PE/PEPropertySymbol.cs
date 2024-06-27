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
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all properties imported from a PE/module.
    /// </summary>
    internal class PEPropertySymbol
        : PropertySymbol
    {
        private readonly string _name;
        private readonly PENamedTypeSymbol _containingType;
        private readonly PropertyDefinitionHandle _handle;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly RefKind _refKind;
        private readonly TypeWithAnnotations _propertyTypeWithAnnotations;
        private readonly PEMethodSymbol _getMethod;
        private readonly PEMethodSymbol _setMethod;
#nullable enable
        private UncommonFields? _uncommonFields;
#nullable disable

        // CONSIDER: the parameters could be computed lazily (as in PEMethodSymbol).
        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the signature (as in PEMethodSymbol).

        // Distinct accessibility value to represent unset.
        private const int UnsetAccessibility = -1;
        private int _declaredAccessibility = UnsetAccessibility;

        private PackedFlags _flags;

        private struct PackedFlags
        {
            // Layout:
            // |....................c|o|d|uu|rr|c|n|s|
            //
            // s = special name flag. 1 bit
            // n = runtime special name flag. 1 bit
            // c = call methods directly flag. 1 bit
            // r = Required member. 2 bits (1 bit for value + 1 completion bit).
            // u = Unscoped ref. 2 bits (1 bit for value + 1 completion bit).
            // d = Use site diagnostic flag. 1 bit
            // o = Obsolete flag. 1 bit
            // c = Custom attributes flag. 1 bit
            private const int IsSpecialNameFlag = 1 << 0;
            private const int IsRuntimeSpecialNameFlag = 1 << 1;
            private const int CallMethodsDirectlyFlag = 1 << 2;
            private const int HasRequiredMemberAttribute = 1 << 4;
            private const int RequiredMemberCompletionBit = 1 << 5;
            private const int HasUnscopedRefAttribute = 1 << 6;
            private const int UnscopedRefCompletionBit = 1 << 7;
            private const int IsUseSiteDiagnosticPopulatedBit = 1 << 8;
            private const int IsObsoleteAttributePopulatedBit = 1 << 9;
            private const int IsCustomAttributesPopulatedBit = 1 << 10;

            private int _bits;

            public PackedFlags(bool isSpecialName, bool isRuntimeSpecialName, bool callMethodsDirectly)
            {
                _bits = (isSpecialName ? IsSpecialNameFlag : 0)
                        | (isRuntimeSpecialName ? IsRuntimeSpecialNameFlag : 0)
                        | (callMethodsDirectly ? CallMethodsDirectlyFlag : 0);
            }

            public void SetHasRequiredMemberAttribute(bool isRequired)
            {
                var bitsToSet = (isRequired ? HasRequiredMemberAttribute : 0) | RequiredMemberCompletionBit;
                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public readonly bool TryGetHasRequiredMemberAttribute(out bool hasRequiredMemberAttribute)
            {
                if ((_bits & RequiredMemberCompletionBit) != 0)
                {
                    hasRequiredMemberAttribute = (_bits & HasRequiredMemberAttribute) != 0;
                    return true;
                }

                hasRequiredMemberAttribute = false;
                return false;
            }

            public void SetHasUnscopedRefAttribute(bool unscopedRef)
            {
                var bitsToSet = (unscopedRef ? HasUnscopedRefAttribute : 0) | UnscopedRefCompletionBit;
                ThreadSafeFlagOperations.Set(ref _bits, bitsToSet);
            }

            public readonly bool TryGetHasUnscopedRefAttribute(out bool hasUnscopedRefAttribute)
            {
                if ((_bits & UnscopedRefCompletionBit) != 0)
                {
                    hasUnscopedRefAttribute = (_bits & HasUnscopedRefAttribute) != 0;
                    return true;
                }

                hasUnscopedRefAttribute = false;
                return false;
            }

            public readonly bool IsSpecialName => (_bits & IsSpecialNameFlag) != 0;
            public readonly bool IsRuntimeSpecialName => (_bits & IsRuntimeSpecialNameFlag) != 0;
            public readonly bool CallMethodsDirectly => (_bits & CallMethodsDirectlyFlag) != 0;

            public void SetUseSiteDiagnosticPopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsUseSiteDiagnosticPopulatedBit);
            }

            public readonly bool IsUseSiteDiagnosticPopulated => (_bits & IsUseSiteDiagnosticPopulatedBit) != 0;

            public void SetObsoleteAttributePopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsObsoleteAttributePopulatedBit);
            }

            public readonly bool IsObsoleteAttributePopulated => (_bits & IsObsoleteAttributePopulatedBit) != 0;

            public void SetCustomAttributesPopulated()
            {
                ThreadSafeFlagOperations.Set(ref _bits, IsCustomAttributesPopulatedBit);
            }

            public readonly bool IsCustomAttributesPopulated => (_bits & IsCustomAttributesPopulatedBit) != 0;
        }

        /// <summary>
        /// This type is used to hold lazily-initialized fields that many properties will not need. We avoid creating it unless one of the fields is needed;
        /// unfortunately, this means that we need to be careful of data races. The general pattern that we use is to check for a flag in <see cref="_flags"/>.
        /// If the flag for that field is set, and there was a positive result (ie, there are indeed custom attributes, or there is obsolete data), then it
        /// is safe to rely on the data in the field. If the flag for a field is set but the result is empty (ie, there is no obsolete data), then we can be in
        /// one of 3 scenarios:
        /// <list type="number">
        /// <item><see cref="_uncommonFields"/> is itself null. In this case, no race has occurred, and the consuming code can safely handle the lack of
        /// <see cref="_uncommonFields"/> however it chooses.</item>
        /// <item><see cref="_uncommonFields"/> is not null, and the backing field has been initialized to some empty value, such as
        /// <see cref="ImmutableArray{T}.Empty"/>. In this case, again, no race has occurred, and the consuming code can simply trust the empty value.</item>
        /// <item><see cref="_uncommonFields"/> is not null, and the backing field is uninitialized, either being <see langword="default" />, or is some
        /// kind of sentinel value. In this case, a data race has occurred, and the consuming code must initialize the field to empty to bring it back
        /// into scenario 2.</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// The initialization pattern for this type <b>must</b> follow the following pattern to make the safety guarantees above:
        /// If the field initialization code determines that the backing field needs to be set to some non-empty value, it <b>must</b> first call <see cref="AccessUncommonFields"/>,
        /// set the backing field using an atomic operation, and then set the flag in <see cref="_flags"/>. This ensures that the field is always set before the flag is set.
        /// If this order is reversed, the consuming code may see the flag set, but the field not initialized, and incorrectly assume that there is no data.
        /// </remarks>
        private sealed class UncommonFields
        {
            public ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
            public Tuple<CultureInfo, string> _lazyDocComment;
            public CachedUseSiteInfo<AssemblySymbol> _lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;
            public ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
        }

        internal static PEPropertySymbol Create(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            PropertyDefinitionHandle handle,
            PEMethodSymbol getMethod,
            PEMethodSymbol setMethod)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!handle.IsNil);

            var metadataDecoder = new MetadataDecoder(moduleSymbol, containingType);
            SignatureHeader callingConvention;
            BadImageFormatException propEx;
            var propertyParams = metadataDecoder.GetSignatureForProperty(handle, out callingConvention, out propEx);
            Debug.Assert(propertyParams.Length > 0);

            var returnInfo = propertyParams[0];

            PEPropertySymbol result = returnInfo.CustomModifiers.IsDefaultOrEmpty && returnInfo.RefCustomModifiers.IsDefaultOrEmpty
                ? new PEPropertySymbol(moduleSymbol, containingType, handle, getMethod, setMethod, propertyParams, metadataDecoder)
                : new PEPropertySymbolWithCustomModifiers(moduleSymbol, containingType, handle, getMethod, setMethod, propertyParams, metadataDecoder);

            // A property should always have this modreq, and vice versa.
            var isBad = (result.RefKind == RefKind.In) != result.RefCustomModifiers.HasInAttributeModifier();

            if (propEx != null || isBad)
            {
                result.AccessUncommonFields()._lazyCachedUseSiteInfo.Initialize(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, result));
                result._flags.SetUseSiteDiagnosticPopulated();
            }

            return result;
        }

        private PEPropertySymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            PropertyDefinitionHandle handle,
            PEMethodSymbol getMethod,
            PEMethodSymbol setMethod,
            ParamInfo<TypeSymbol>[] propertyParams,
            MetadataDecoder metadataDecoder)
        {
            _containingType = containingType;
            var module = moduleSymbol.Module;
            PropertyAttributes mdFlags = 0;
            BadImageFormatException mrEx = null;

            try
            {
                module.GetPropertyDefPropsOrThrow(handle, out _name, out mdFlags);
            }
            catch (BadImageFormatException e)
            {
                mrEx = e;

                if ((object)_name == null)
                {
                    _name = string.Empty;
                }
            }

            _getMethod = getMethod;
            _setMethod = setMethod;
            _handle = handle;

            SignatureHeader unusedCallingConvention;
            BadImageFormatException getEx = null;
            var getMethodParams = (object)getMethod == null ? null : metadataDecoder.GetSignatureForMethod(getMethod.Handle, out unusedCallingConvention, out getEx);
            BadImageFormatException setEx = null;
            var setMethodParams = (object)setMethod == null ? null : metadataDecoder.GetSignatureForMethod(setMethod.Handle, out unusedCallingConvention, out setEx);

            // NOTE: property parameter names are not recorded in metadata, so we have to
            // use the parameter names from one of the indexers
            // NB: prefer setter names to getter names if both are present.
            bool isBad;

            _parameters = setMethodParams is null
                ? GetParameters(moduleSymbol, this, getMethod, propertyParams, getMethodParams, out isBad)
                : GetParameters(moduleSymbol, this, setMethod, propertyParams, setMethodParams, out isBad);

            if (getEx != null || setEx != null || mrEx != null || isBad)
            {
                AccessUncommonFields()._lazyCachedUseSiteInfo.Initialize(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this));
                _flags.SetUseSiteDiagnosticPopulated();
            }

            var returnInfo = propertyParams[0];
            var typeCustomModifiers = CSharpCustomModifier.Convert(returnInfo.CustomModifiers);

            if (returnInfo.IsByRef)
            {
                if (moduleSymbol.Module.HasIsReadOnlyAttribute(handle))
                {
                    _refKind = RefKind.RefReadOnly;
                }
                else
                {
                    _refKind = RefKind.Ref;
                }
            }
            else
            {
                _refKind = RefKind.None;
            }

            // CONSIDER: Can we make parameter type computation lazy?
            TypeSymbol originalPropertyType = returnInfo.Type;

            originalPropertyType = DynamicTypeDecoder.TransformType(originalPropertyType, typeCustomModifiers.Length, handle, moduleSymbol, _refKind);
            originalPropertyType = NativeIntegerTypeDecoder.TransformType(originalPropertyType, handle, moduleSymbol, _containingType);

            // Dynamify object type if necessary
            originalPropertyType = originalPropertyType.AsDynamicIfNoPia(_containingType);

            // We start without annotation (they will be decoded below)
            var propertyTypeWithAnnotations = TypeWithAnnotations.Create(originalPropertyType, customModifiers: typeCustomModifiers);

            // Decode nullable before tuple types to avoid converting between
            // NamedTypeSymbol and TupleTypeSymbol unnecessarily.

            // The containing type is passed to NullableTypeDecoder.TransformType to determine access
            // because the property does not have explicit accessibility in metadata.
            propertyTypeWithAnnotations = NullableTypeDecoder.TransformType(propertyTypeWithAnnotations, handle, moduleSymbol, accessSymbol: _containingType, nullableContext: _containingType);
            propertyTypeWithAnnotations = TupleTypeDecoder.DecodeTupleTypesIfApplicable(propertyTypeWithAnnotations, handle, moduleSymbol);

            _propertyTypeWithAnnotations = propertyTypeWithAnnotations;

            // A property is bogus and must be accessed by calling its accessors directly if the
            // accessor signatures do not agree, both with each other and with the property,
            // or if it has parameters and is not an indexer or indexed property.
            bool callMethodsDirectly = !DoSignaturesMatch(module, metadataDecoder, propertyParams, _getMethod, getMethodParams, _setMethod, setMethodParams) ||
                MustCallMethodsDirectlyCore() ||
                anyUnexpectedRequiredModifiers(propertyParams);

            if (!callMethodsDirectly)
            {
                if ((object)_getMethod != null)
                {
                    _getMethod.SetAssociatedProperty(this, MethodKind.PropertyGet);
                }

                if ((object)_setMethod != null)
                {
                    _setMethod.SetAssociatedProperty(this, MethodKind.PropertySet);
                }
            }

            _flags = new PackedFlags(
                isSpecialName: (mdFlags & PropertyAttributes.SpecialName) != 0,
                isRuntimeSpecialName: (mdFlags & PropertyAttributes.RTSpecialName) != 0,
                callMethodsDirectly);

            static bool anyUnexpectedRequiredModifiers(ParamInfo<TypeSymbol>[] propertyParams)
            {
                return propertyParams.Any(p => (!p.RefCustomModifiers.IsDefaultOrEmpty && p.RefCustomModifiers.Any(static m => !m.IsOptional && !m.Modifier.IsWellKnownTypeInAttribute())) ||
                                               p.CustomModifiers.AnyRequired());
            }
        }

        private UncommonFields AccessUncommonFields()
        {
            var retVal = _uncommonFields;
            return retVal ?? InterlockedOperations.Initialize(ref _uncommonFields, createUncommonFields());

            UncommonFields createUncommonFields()
            {
                var retVal = new UncommonFields();
                if (!_flags.IsObsoleteAttributePopulated)
                {
                    retVal._lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
                }

                if (!_flags.IsUseSiteDiagnosticPopulated)
                {
                    retVal._lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;
                }

                if (_flags.IsCustomAttributesPopulated)
                {
                    retVal._lazyCustomAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                }

                return retVal;
            }
        }

        private bool MustCallMethodsDirectlyCore()
        {
            if (this.RefKind != RefKind.None && _setMethod != null)
            {
                return true;
            }
            else if (this.ParameterCount == 0)
            {
                return false;
            }
            else if (this.IsIndexedProperty)
            {
                return this.IsStatic;
            }
            else if (this.IsIndexer)
            {
                return this.HasRefOrOutParameter();
            }
            else
            {
                return true;
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

        /// <remarks>
        /// To facilitate lookup, all indexer symbols have the same name.
        /// Check the MetadataName property to find the name we imported.
        /// </remarks>
        public override string Name
        {
            get { return this.IsIndexer ? WellKnownMemberNames.Indexer : _name; }
        }

        internal override bool HasSpecialName
        {
            get { return _flags.IsSpecialName; }
        }

        public override string MetadataName
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

        internal PropertyDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (_declaredAccessibility == UnsetAccessibility)
                {
                    Accessibility accessibility;
                    if (this.IsOverride)
                    {
                        // Determining the accessibility of an overriding property is tricky.  It should be
                        // based on the accessibilities of the accessors, but the overriding property need
                        // not override both accessors.  As a result, we may need to look at the accessors
                        // of an overridden method.
                        //
                        // One might assume that we could just go straight to the least-derived 
                        // property (i.e. the original virtual property) and check its accessors, but
                        // that can yield incorrect results if the least-derived property is in a
                        // different assembly.  For any overridden and (directly) overriding members, M and M',
                        // in different assemblies, A1 and A2, if M is protected internal, then M' must be 
                        // protected internal if the internals of A1 are visible to A2 and protected otherwise.
                        //
                        // Therefore, if we cross an assembly boundary in the course of walking up the
                        // override chain, and if the overriding assembly cannot see the internals of the
                        // overridden assembly, then any protected internal accessors we find should be 
                        // treated as protected, for the purposes of determining property accessibility.
                        //
                        // NOTE: This process has no effect on accessor accessibility - a protected internal
                        // accessor in another assembly will still have declared accessibility protected internal.
                        // The difference between the accessibilities of the overriding and overridden accessors
                        // will be accommodated later, when we check for CS0507 (ERR_CantChangeAccessOnOverride).

                        bool crossedAssemblyBoundaryWithoutInternalsVisibleTo = false;
                        Accessibility getAccessibility = Accessibility.NotApplicable;
                        Accessibility setAccessibility = Accessibility.NotApplicable;
                        PropertySymbol curr = this;
                        while (true)
                        {
                            if (getAccessibility == Accessibility.NotApplicable)
                            {
                                MethodSymbol getMethod = curr.GetMethod;
                                if ((object)getMethod != null)
                                {
                                    Accessibility overriddenAccessibility = getMethod.DeclaredAccessibility;
                                    getAccessibility = overriddenAccessibility == Accessibility.ProtectedOrInternal && crossedAssemblyBoundaryWithoutInternalsVisibleTo
                                        ? Accessibility.Protected
                                        : overriddenAccessibility;
                                }
                            }

                            if (setAccessibility == Accessibility.NotApplicable)
                            {
                                MethodSymbol setMethod = curr.SetMethod;
                                if ((object)setMethod != null)
                                {
                                    Accessibility overriddenAccessibility = setMethod.DeclaredAccessibility;
                                    setAccessibility = overriddenAccessibility == Accessibility.ProtectedOrInternal && crossedAssemblyBoundaryWithoutInternalsVisibleTo
                                        ? Accessibility.Protected
                                        : overriddenAccessibility;
                                }
                            }

                            if (getAccessibility != Accessibility.NotApplicable && setAccessibility != Accessibility.NotApplicable)
                            {
                                break;
                            }

                            PropertySymbol next = curr.OverriddenProperty;

                            if ((object)next == null)
                            {
                                break;
                            }

                            if (!crossedAssemblyBoundaryWithoutInternalsVisibleTo && !curr.ContainingAssembly.HasInternalAccessTo(next.ContainingAssembly))
                            {
                                crossedAssemblyBoundaryWithoutInternalsVisibleTo = true;
                            }

                            curr = next;
                        }

                        accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(getAccessibility, setAccessibility);
                    }
                    else
                    {
                        accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(this.GetMethod, this.SetMethod);
                    }

                    Interlocked.CompareExchange(ref _declaredAccessibility, (int)accessibility, UnsetAccessibility);
                }

                return (Accessibility)_declaredAccessibility;
            }
        }

        public override bool IsExtern
        {
            get
            {
                // Some accessor extern.
                return
                    ((object)_getMethod != null && _getMethod.IsExtern) ||
                    ((object)_setMethod != null && _setMethod.IsExtern);
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Some accessor abstract.
                return
                    ((object)_getMethod != null && _getMethod.IsAbstract) ||
                    ((object)_setMethod != null && _setMethod.IsAbstract);
            }
        }

        public override bool IsSealed
        {
            get
            {
                // All accessors sealed.
                return
                    ((object)_getMethod == null || _getMethod.IsSealed) &&
                    ((object)_setMethod == null || _setMethod.IsSealed);
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // Some accessor virtual (as long as another isn't override or abstract).
                return !IsOverride && !IsAbstract &&
                    (((object)_getMethod != null && _getMethod.IsVirtual) ||
                     ((object)_setMethod != null && _setMethod.IsVirtual));
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Some accessor override.
                return
                    ((object)_getMethod != null && _getMethod.IsOverride) ||
                    ((object)_setMethod != null && _setMethod.IsOverride);
            }
        }

        public override bool IsStatic
        {
            get
            {
                // All accessors static.
                return
                    ((object)_getMethod == null || _getMethod.IsStatic) &&
                    ((object)_setMethod == null || _setMethod.IsStatic);
            }
        }

        internal override bool IsRequired
        {
            get
            {
                if (!_flags.TryGetHasRequiredMemberAttribute(out bool hasRequiredMemberAttribute))
                {
                    var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                    hasRequiredMemberAttribute = containingPEModuleSymbol.Module.HasAttribute(_handle, AttributeDescription.RequiredMemberAttribute);
                    _flags.SetHasRequiredMemberAttribute(hasRequiredMemberAttribute);
                }

                return hasRequiredMemberAttribute;
            }
        }

        internal sealed override bool HasUnscopedRefAttribute
        {
            get
            {
                if (!_flags.TryGetHasUnscopedRefAttribute(out bool hasUnscopedRefAttribute))
                {
                    var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                    hasUnscopedRefAttribute = containingPEModuleSymbol.Module.HasUnscopedRefAttribute(_handle);
                    _flags.SetHasUnscopedRefAttribute(hasUnscopedRefAttribute);
                }

                return hasUnscopedRefAttribute;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        /// <remarks>
        /// This property can return true for bogus indexers.
        /// Rationale: If a type in metadata has a single, bogus indexer
        /// and a source method tries to invoke it, then Dev10 reports a bogus
        /// indexer rather than lack of an indexer.
        /// </remarks>
        public override bool IsIndexer
        {
            get
            {
                // NOTE: Dev10 appears to include static indexers in overload resolution 
                // for an array access expression, so it stands to reason that it considers
                // them indexers.
                if (this.ParameterCount > 0)
                {
                    string defaultMemberName = _containingType.DefaultMemberName;
                    return _name == defaultMemberName || //NB: not Name property (break mutual recursion)
                        ((object)this.GetMethod != null && this.GetMethod.Name == defaultMemberName) ||
                        ((object)this.SetMethod != null && this.SetMethod.Name == defaultMemberName);
                }
                return false;
            }
        }

        public override bool IsIndexedProperty
        {
            get
            {
                // Indexed property support is limited to types marked [ComImport],
                // to match the native compiler where the feature was scoped to
                // avoid supporting property groups.
                return (this.ParameterCount > 0) && _containingType.IsComImport;
            }
        }

        public override RefKind RefKind
        {
            get { return _refKind; }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _propertyTypeWithAnnotations; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override MethodSymbol GetMethod
        {
            get { return _getMethod; }
        }

        public override MethodSymbol SetMethod
        {
            get { return _setMethod; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                var metadataDecoder = new MetadataDecoder(_containingType.ContainingPEModule, _containingType);
                return (Microsoft.Cci.CallingConvention)(metadataDecoder.GetSignatureHeaderForProperty(_handle).RawValue);
            }
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

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (!_flags.IsCustomAttributesPopulated)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;

                ImmutableArray<CSharpAttributeData> attributes = containingPEModuleSymbol.GetCustomAttributesForToken(
                      _handle,
                      out _,
                      this.RefKind == RefKind.RefReadOnly ? AttributeDescription.IsReadOnlyAttribute : default,
                      out CustomAttributeHandle required,
                      AttributeDescription.RequiredMemberAttribute);

                if (!attributes.IsEmpty)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref AccessUncommonFields()._lazyCustomAttributes, attributes);
                }

                _flags.SetCustomAttributesPopulated();
                _flags.SetHasRequiredMemberAttribute(!required.IsNil);
            }

            var uncommonFields = _uncommonFields;
            if (uncommonFields == null)
            {
                return ImmutableArray<CSharpAttributeData>.Empty;
            }
            else
            {
                var result = uncommonFields._lazyCustomAttributes;
                if (result.IsDefault)
                {
                    result = ImmutableArray<CSharpAttributeData>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref uncommonFields._lazyCustomAttributes, result);
                }

                return result;
            }
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return GetAttributes();
        }

        /// <summary>
        /// Intended behavior: this property, P, explicitly implements an interface property, IP, 
        /// if any of the following is true:
        /// 
        /// 1) P.get explicitly implements IP.get and P.set explicitly implements IP.set
        /// 2) P.get explicitly implements IP.get and there is no IP.set
        /// 3) P.set explicitly implements IP.set and there is no IP.get
        /// 
        /// Extra or missing accessors will not result in errors, P will simply not report that
        /// it explicitly implements IP.
        /// </summary>
        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (((object)_getMethod == null || _getMethod.ExplicitInterfaceImplementations.Length == 0) &&
                    ((object)_setMethod == null || _setMethod.ExplicitInterfaceImplementations.Length == 0))
                {
                    return ImmutableArray<PropertySymbol>.Empty;
                }

                var propertiesWithImplementedGetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(_getMethod);
                var propertiesWithImplementedSetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(_setMethod);

                var builder = ArrayBuilder<PropertySymbol>.GetInstance();

                foreach (var prop in propertiesWithImplementedGetters)
                {
                    if (!prop.SetMethod.IsImplementable() || propertiesWithImplementedSetters.Contains(prop))
                    {
                        builder.Add(prop);
                    }
                }

                foreach (var prop in propertiesWithImplementedSetters)
                {
                    // No need to worry about duplicates.  If prop was added by the previous loop,
                    // then it must have a GetMethod.
                    if (!prop.GetMethod.IsImplementable())
                    {
                        builder.Add(prop);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return _flags.CallMethodsDirectly; }
        }

        private static bool DoSignaturesMatch(
            PEModule module,
            MetadataDecoder metadataDecoder,
            ParamInfo<TypeSymbol>[] propertyParams,
            PEMethodSymbol getMethod,
            ParamInfo<TypeSymbol>[] getMethodParams,
            PEMethodSymbol setMethod,
            ParamInfo<TypeSymbol>[] setMethodParams)
        {
            Debug.Assert((getMethodParams == null) == ((object)getMethod == null));
            Debug.Assert((setMethodParams == null) == ((object)setMethod == null));

            bool hasGetMethod = getMethodParams != null;
            bool hasSetMethod = setMethodParams != null;

            if (hasGetMethod && !metadataDecoder.DoPropertySignaturesMatch(propertyParams, getMethodParams, comparingToSetter: false, compareParamByRef: true, compareReturnType: true))
            {
                return false;
            }

            if (hasSetMethod && !metadataDecoder.DoPropertySignaturesMatch(propertyParams, setMethodParams, comparingToSetter: true, compareParamByRef: true, compareReturnType: true))
            {
                return false;
            }

            if (hasGetMethod && hasSetMethod)
            {
                var lastPropertyParamIndex = propertyParams.Length - 1;
                var getHandle = getMethodParams[lastPropertyParamIndex].Handle;
                var setHandle = setMethodParams[lastPropertyParamIndex].Handle;
                var getterHasParamArray = !getHandle.IsNil && module.HasParamArrayAttribute(getHandle);
                var setterHasParamArray = !setHandle.IsNil && module.HasParamArrayAttribute(setHandle);
                var getterHasParamCollection = !getHandle.IsNil && module.HasParamCollectionAttribute(getHandle);
                var setterHasParamCollection = !setHandle.IsNil && module.HasParamCollectionAttribute(setHandle);
                if (getterHasParamArray != setterHasParamArray || getterHasParamCollection != setterHasParamCollection)
                {
                    return false;
                }

                if ((getMethod.IsExtern != setMethod.IsExtern) ||
                    // (getMethod.IsAbstract != setMethod.IsAbstract) || // NOTE: Dev10 accepts one abstract accessor
                    (getMethod.IsSealed != setMethod.IsSealed) ||
                    (getMethod.IsOverride != setMethod.IsOverride) ||
                    (getMethod.IsStatic != setMethod.IsStatic))
                {
                    return false;
                }
            }

            return true;
        }

        private static ImmutableArray<ParameterSymbol> GetParameters(
            PEModuleSymbol moduleSymbol,
            PEPropertySymbol property,
            PEMethodSymbol accessor,
            ParamInfo<TypeSymbol>[] propertyParams,
            ParamInfo<TypeSymbol>[] accessorParams,
            out bool anyParameterIsBad)
        {
            anyParameterIsBad = false;

            // First parameter is the property type.
            if (propertyParams.Length < 2)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }

            var numAccessorParams = accessorParams.Length;

            var parameters = new ParameterSymbol[propertyParams.Length - 1];
            for (int i = 1; i < propertyParams.Length; i++) // from 1 to skip property/return type
            {
                // NOTE: this is a best guess at the Dev10 behavior.  The actual behavior is
                // in the unmanaged helper code that Dev10 uses to load the metadata.
                var propertyParam = propertyParams[i];
                ParameterHandle paramHandle;
                Symbol nullableContext;
                if (i < numAccessorParams)
                {
                    paramHandle = accessorParams[i].Handle;
                    nullableContext = accessor;
                }
                else
                {
                    paramHandle = propertyParam.Handle;
                    nullableContext = property;
                }
                var ordinal = i - 1;
                bool isBad;

                parameters[ordinal] = PEParameterSymbol.Create(moduleSymbol, property, accessor.IsMetadataVirtual(), ordinal, paramHandle, propertyParam, nullableContext, out isBad);

                if (isBad)
                {
                    anyParameterIsBad = true;
                }
            }

            return parameters.AsImmutableOrNull();
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref AccessUncommonFields()._lazyDocComment);
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            AssemblySymbol primaryDependency = PrimaryDependency;

            if (!_flags.IsUseSiteDiagnosticPopulated)
            {
                var result = new UseSiteInfo<AssemblySymbol>(primaryDependency);
                CalculateUseSiteDiagnostic(ref result);
                var diag = deriveCompilerFeatureRequiredUseSiteInfo();
                MergeUseSiteDiagnostics(ref diag, result.DiagnosticInfo);
                result = result.AdjustDiagnosticInfo(diag);

                if (result.DiagnosticInfo is not null || !result.SecondaryDependencies.IsNullOrEmpty())
                {
                    AccessUncommonFields()._lazyCachedUseSiteInfo.InterlockedInitializeFromSentinel(PrimaryDependency, result);
                }

                _flags.SetUseSiteDiagnosticPopulated();
            }

            var uncommonFields = _uncommonFields;
            if (uncommonFields == null)
            {
                return new UseSiteInfo<AssemblySymbol>(primaryDependency);
            }
            else
            {
                var result = uncommonFields._lazyCachedUseSiteInfo;
                if (!result.IsInitialized)
                {
                    uncommonFields._lazyCachedUseSiteInfo.InterlockedInitializeFromSentinel(primaryDependency, new UseSiteInfo<AssemblySymbol>(primaryDependency));
                    result = uncommonFields._lazyCachedUseSiteInfo;
                }

                return result.ToUseSiteInfo(primaryDependency);
            }

            DiagnosticInfo deriveCompilerFeatureRequiredUseSiteInfo()
            {
                var containingType = (PENamedTypeSymbol)ContainingType;
                PEModuleSymbol containingPEModule = _containingType.ContainingPEModule;
                var decoder = new MetadataDecoder(containingPEModule, containingType);
                var diag = PEUtilities.DeriveCompilerFeatureRequiredAttributeDiagnostic(
                    this,
                    containingPEModule,
                    Handle,
                    allowedFeatures: CompilerFeatureRequiredFeatures.None,
                    decoder);

                if (diag != null)
                {
                    return diag;
                }

                foreach (var param in Parameters)
                {
                    diag = ((PEParameterSymbol)param).DeriveCompilerFeatureRequiredDiagnostic(decoder);
                    if (diag != null)
                    {
                        return diag;
                    }
                }

                return containingType.GetCompilerFeatureRequiredDiagnostic();
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                if (!_flags.IsObsoleteAttributePopulated)
                {
                    var result = ObsoleteAttributeHelpers.GetObsoleteDataFromMetadata(_handle, (PEModuleSymbol)(this.ContainingModule), ignoreByRefLikeMarker: false, ignoreRequiredMemberMarker: false);
                    if (result != null)
                    {
                        result = InterlockedOperations.Initialize(ref AccessUncommonFields()._lazyObsoleteAttributeData, result, ObsoleteAttributeData.Uninitialized);
                    }

                    _flags.SetObsoleteAttributePopulated();
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
                        ? InterlockedOperations.Initialize(ref uncommonFields._lazyObsoleteAttributeData, initializedValue: null, ObsoleteAttributeData.Uninitialized)
                        : result;
                }
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _flags.IsRuntimeSpecialName;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        private sealed class PEPropertySymbolWithCustomModifiers : PEPropertySymbol
        {
            private readonly ImmutableArray<CustomModifier> _refCustomModifiers;

            public PEPropertySymbolWithCustomModifiers(
                PEModuleSymbol moduleSymbol,
                PENamedTypeSymbol containingType,
                PropertyDefinitionHandle handle,
                PEMethodSymbol getMethod,
                PEMethodSymbol setMethod,
                ParamInfo<TypeSymbol>[] propertyParams,
                MetadataDecoder metadataDecoder)
                : base(moduleSymbol, containingType, handle, getMethod, setMethod,
                    propertyParams,
                    metadataDecoder)
            {
                var returnInfo = propertyParams[0];
                _refCustomModifiers = CSharpCustomModifier.Convert(returnInfo.RefCustomModifiers);
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get { return _refCustomModifiers; }
            }
        }
    }
}
