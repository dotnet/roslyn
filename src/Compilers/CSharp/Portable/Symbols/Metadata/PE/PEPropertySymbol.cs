// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all properties imported from a PE/module.
    /// </summary>
    internal sealed class PEPropertySymbol
        : PropertySymbol
    {
        private readonly string _name;
        private readonly PENamedTypeSymbol _containingType;
        private readonly PropertyDefinitionHandle _handle;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly RefKind _refKind;
        private readonly TypeSymbolWithAnnotations _propertyType;
        private readonly PEMethodSymbol _getMethod;
        private readonly PEMethodSymbol _setMethod;
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private Tuple<CultureInfo, string> _lazyDocComment;
        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private ObsoleteAttributeData _lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        // CONSIDER: the parameters could be computed lazily (as in PEMethodSymbol).
        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the signature (as in PEMethodSymbol).

        // Distinct accessibility value to represent unset.
        private const int UnsetAccessibility = -1;
        private int _declaredAccessibility = UnsetAccessibility;

        private readonly Flags _flags;

        [Flags]
        private enum Flags : byte
        {
            IsSpecialName = 1,
            IsRuntimeSpecialName = 2,
            CallMethodsDirectly = 4
        }

        internal PEPropertySymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            PropertyDefinitionHandle handle,
            PEMethodSymbol getMethod,
            PEMethodSymbol setMethod)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!handle.IsNil);

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

            var metadataDecoder = new MetadataDecoder(moduleSymbol, containingType);
            SignatureHeader callingConvention;
            BadImageFormatException propEx;
            var propertyParams = metadataDecoder.GetSignatureForProperty(handle, out callingConvention, out propEx, allowByRefReturn: true);
            Debug.Assert(propertyParams.Length > 0);

            SignatureHeader unusedCallingConvention;
            BadImageFormatException getEx = null;
            var getMethodParams = (object)getMethod == null ? null : metadataDecoder.GetSignatureForMethod(getMethod.Handle, out unusedCallingConvention, out getEx, allowByRefReturn: true);
            BadImageFormatException setEx = null;
            var setMethodParams = (object)setMethod == null ? null : metadataDecoder.GetSignatureForMethod(setMethod.Handle, out unusedCallingConvention, out setEx, allowByRefReturn: false);

            // NOTE: property parameter names are not recorded in metadata, so we have to
            // use the parameter names from one of the indexers
            // NB: prefer setter names to getter names if both are present.
            bool isBad;
            _parameters = GetParameters(moduleSymbol, this, propertyParams, setMethodParams ?? getMethodParams, out isBad);

            if (propEx != null || getEx != null || setEx != null || mrEx != null || isBad)
            {
                _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            var typeCustomModifiers = CSharpCustomModifier.Convert(propertyParams[0].CustomModifiers);

            _refKind = propertyParams[0].IsByRef ? RefKind.Ref : RefKind.None;

            // CONSIDER: Can we make parameter type computation lazy?
            TypeSymbol originalPropertyType = propertyParams[0].Type;
            TypeSymbol propertyType = DynamicTypeDecoder.TransformType(originalPropertyType, typeCustomModifiers.Length, handle, moduleSymbol);

            // Dynamify object type if necessary
            _propertyType = TypeSymbolWithAnnotations.Create(propertyType.AsDynamicIfNoPia(_containingType), typeCustomModifiers);

            // A property is bogus and must be accessed by calling its accessors directly if the
            // accessor signatures do not agree, both with each other and with the property,
            // or if it has parameters and is not an indexer or indexed property.
            bool callMethodsDirectly = !DoSignaturesMatch(module, metadataDecoder, propertyParams, _getMethod, getMethodParams, _setMethod, setMethodParams) ||
                MustCallMethodsDirectlyCore();

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

            if (callMethodsDirectly)
            {
                _flags |= Flags.CallMethodsDirectly;
            }

            if ((mdFlags & PropertyAttributes.SpecialName) != 0)
            {
                _flags |= Flags.IsSpecialName;
            }

            if ((mdFlags & PropertyAttributes.RTSpecialName) != 0)
            {
                _flags |= Flags.IsRuntimeSpecialName;
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
            get { return (_flags & Flags.IsSpecialName) != 0; }
        }

        public override string MetadataName
        {
            get
            {
                return _name;
            }
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

        internal override RefKind RefKind
        {
            get { return _refKind; }
        }

        public override TypeSymbolWithAnnotations Type
        {
            get { return _propertyType; }
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
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                containingPEModuleSymbol.LoadCustomAttributes(_handle, ref _lazyCustomAttributes);
            }
            return _lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
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
                    if ((object)prop.SetMethod == null || propertiesWithImplementedSetters.Contains(prop))
                    {
                        builder.Add(prop);
                    }
                }

                foreach (var prop in propertiesWithImplementedSetters)
                {
                    // No need to worry about duplicates.  If prop was added by the previous loop,
                    // then it must have a GetMethod.
                    if ((object)prop.GetMethod == null)
                    {
                        builder.Add(prop);
                    }
                }

                return builder.ToImmutableAndFree();
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return (_flags & Flags.CallMethodsDirectly) != 0; }
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
                var getterHasParamArray = !getHandle.IsNil && module.HasParamsAttribute(getHandle);
                var setterHasParamArray = !setHandle.IsNil && module.HasParamsAttribute(setHandle);
                if (getterHasParamArray != setterHasParamArray)
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
                var paramHandle = i < numAccessorParams ? accessorParams[i].Handle : propertyParam.Handle;
                var ordinal = i - 1;
                bool isBad;
                parameters[ordinal] = PEParameterSymbol.Create(moduleSymbol, property, ordinal, paramHandle, propertyParam, out isBad);

                if (isBad)
                {
                    anyParameterIsBad = true;
                }
            }
            return parameters.AsImmutableOrNull();
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, _containingType.ContainingPEModule, preferredCulture, cancellationToken, ref _lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref _lazyObsoleteAttributeData, _handle, (PEModuleSymbol)(this.ContainingModule));
                return _lazyObsoleteAttributeData;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (_flags & Flags.IsRuntimeSpecialName) != 0;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
