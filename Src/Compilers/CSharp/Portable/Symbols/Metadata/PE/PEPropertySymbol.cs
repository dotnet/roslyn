// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly string name;
        private readonly PENamedTypeSymbol containingType;
        private readonly PropertyHandle handle;
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol propertyType;
        private readonly PEMethodSymbol getMethod;
        private readonly PEMethodSymbol setMethod;
        private readonly ImmutableArray<CustomModifier> typeCustomModifiers;
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
        private Tuple<CultureInfo, string> lazyDocComment;
        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private ObsoleteAttributeData lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;

        // CONSIDER: the parameters could be computed lazily (as in PEMethodSymbol).
        // CONSIDER: if the parameters were computed lazily, ParameterCount could be overridden to fall back on the signature (as in PEMethodSymbol).

        // Distinct accessibility value to represent unset.
        private const int UnsetAccessibility = -1;
        private int declaredAccessibility = UnsetAccessibility;

        private readonly Flags flags;

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
            PropertyHandle handle,
            PEMethodSymbol getMethod,
            PEMethodSymbol setMethod)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)containingType != null);
            Debug.Assert(!handle.IsNil);

            this.containingType = containingType;
            var module = moduleSymbol.Module;
            PropertyAttributes mdFlags = 0;
            BadImageFormatException mrEx = null;

            try
            {
                module.GetPropertyDefPropsOrThrow(handle, out this.name, out mdFlags);
            }
            catch (BadImageFormatException e)
            {
                mrEx = e;

                if ((object)this.name == null)
                {
                    this.name = string.Empty;
                }
            }

            this.getMethod = getMethod;
            this.setMethod = setMethod;
            this.handle = handle;

            var metadataDecoder = new MetadataDecoder(moduleSymbol, containingType);
            byte callingConvention;
            BadImageFormatException propEx;
            var propertyParams = metadataDecoder.GetSignatureForProperty(handle, out callingConvention, out propEx);
            Debug.Assert(propertyParams.Length > 0);

            byte unusedCallingConvention;
            BadImageFormatException getEx = null;
            var getMethodParams = (object)getMethod == null ? null : metadataDecoder.GetSignatureForMethod(getMethod.Handle, out unusedCallingConvention, out getEx);
            BadImageFormatException setEx = null;
            var setMethodParams = (object)setMethod == null ? null : metadataDecoder.GetSignatureForMethod(setMethod.Handle, out unusedCallingConvention, out setEx);

            // NOTE: property parameter names are not recorded in metadata, so we have to
            // use the parameter names from one of the indexers.
            // NB: prefer setter names to getter names if both are present.
            bool isBad;
            this.parameters = GetParameters(moduleSymbol, this, propertyParams, setMethodParams ?? getMethodParams, out isBad);

            if (propEx != null || getEx != null || setEx != null || mrEx != null || isBad)
            {
                lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            this.typeCustomModifiers = CSharpCustomModifier.Convert(propertyParams[0].CustomModifiers);

            // CONSIDER: Can we make parameter type computation lazy?
            TypeSymbol originalPropertyType = propertyParams[0].Type;
            this.propertyType = DynamicTypeDecoder.TransformType(originalPropertyType, typeCustomModifiers.Length, handle, moduleSymbol);

            // Dynamify object type if necessary
            this.propertyType = this.propertyType.AsDynamicIfNoPia(this.containingType);

            // A property is bogus and must be accessed by calling its accessors directly if the
            // accessor signatures do not agree, both with each other and with the property,
            // or if it has parameters and is not an indexer or indexed property.
            bool callMethodsDirectly = !DoSignaturesMatch(module, metadataDecoder, propertyParams, this.getMethod, getMethodParams, this.setMethod, setMethodParams) ||
                MustCallMethodsDirectlyCore();

            if (!callMethodsDirectly)
            {
                if ((object)this.getMethod != null)
                {
                    this.getMethod.SetAssociatedProperty(this, MethodKind.PropertyGet);
                }

                if ((object)this.setMethod != null)
                {
                    this.setMethod.SetAssociatedProperty(this, MethodKind.PropertySet);
                }
            }

            if (callMethodsDirectly)
            {
                flags |= Flags.CallMethodsDirectly;
            }

            if ((mdFlags & PropertyAttributes.SpecialName) != 0)
            {
                flags |= Flags.IsSpecialName;
            }

            if ((mdFlags & PropertyAttributes.RTSpecialName) != 0)
            {
                flags |= Flags.IsRuntimeSpecialName;
            }
        }

        private bool MustCallMethodsDirectlyCore()
        {
            if (this.ParameterCount == 0)
            {
                return false;
            }
            if (this.IsIndexedProperty)
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
                return this.containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        /// <remarks>
        /// To facilitate lookup, all indexer symbols have the same name.
        /// Check the MetadataName property to find the name we imported.
        /// </remarks>
        public override string Name
        {
            get { return this.IsIndexer ? WellKnownMemberNames.Indexer : name; }
        }

        internal override bool HasSpecialName
        {
            get { return (flags & Flags.IsSpecialName) != 0; }
        }

        public override string MetadataName
        {
            get
            {
                return this.name;
            }
        }
        internal PropertyHandle Handle
        {
            get
            {
                return this.handle;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (this.declaredAccessibility == UnsetAccessibility)
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

                    Interlocked.CompareExchange(ref this.declaredAccessibility, (int)accessibility, UnsetAccessibility);
                }

                return (Accessibility)this.declaredAccessibility;
            }
        }

        public override bool IsExtern
        {
            get
            {
                // Some accessor extern.
                return
                    ((object)getMethod != null && getMethod.IsExtern) ||
                    ((object)setMethod != null && setMethod.IsExtern);
            }
        }

        public override bool IsAbstract
        {
            get
            {
                // Some accessor abstract.
                return
                    ((object)getMethod != null && getMethod.IsAbstract) ||
                    ((object)setMethod != null && setMethod.IsAbstract);
            }
        }

        public override bool IsSealed
        {
            get
            {
                // All accessors sealed.
                return
                    ((object)getMethod == null || getMethod.IsSealed) &&
                    ((object)setMethod == null || setMethod.IsSealed);
            }
        }

        public override bool IsVirtual
        {
            get
            {
                // Some accessor virtual (as long as another isn't override or abstract).
                return !IsOverride && !IsAbstract &&
                    (((object)getMethod != null && getMethod.IsVirtual) ||
                     ((object)setMethod != null && setMethod.IsVirtual));
            }
        }

        public override bool IsOverride
        {
            get
            {
                // Some accessor override.
                return
                    ((object)getMethod != null && getMethod.IsOverride) ||
                    ((object)setMethod != null && setMethod.IsOverride);
            }
        }

        public override bool IsStatic
        {
            get
            {
                // All accessors static.
                return
                    ((object)getMethod == null || getMethod.IsStatic) &&
                    ((object)setMethod == null || setMethod.IsStatic);
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return this.parameters; }
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
                    string defaultMemberName = this.containingType.DefaultMemberName;
                    return this.name == defaultMemberName || //NB: not Name property (break mutual recursion)
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
                return (this.ParameterCount > 0) && this.containingType.IsComImport;
            }
        }

        public override TypeSymbol Type
        {
            get { return this.propertyType; }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get { return typeCustomModifiers; }
        }

        public override MethodSymbol GetMethod
        {
            get { return this.getMethod; }
        }

        public override MethodSymbol SetMethod
        {
            get { return this.setMethod; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                var metadataDecoder = new MetadataDecoder(containingType.ContainingPEModule, containingType);
                return (Microsoft.Cci.CallingConvention)metadataDecoder.GetCallingConventionForProperty(handle);
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
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                containingPEModuleSymbol.LoadCustomAttributes(this.handle, ref this.lazyCustomAttributes);
            }
            return this.lazyCustomAttributes;
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
                if (((object)this.getMethod == null || this.getMethod.ExplicitInterfaceImplementations.Length == 0) &&
                    ((object)this.setMethod == null || this.setMethod.ExplicitInterfaceImplementations.Length == 0))
                {
                    return ImmutableArray<PropertySymbol>.Empty;
                }

                var propertiesWithImplementedGetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(this.getMethod);
                var propertiesWithImplementedSetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(this.setMethod);

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
            get { return (flags & Flags.CallMethodsDirectly) != 0; }
        }

        private static bool DoSignaturesMatch(
            PEModule module,
            MetadataDecoder metadataDecoder,
            MetadataDecoder.ParamInfo[] propertyParams,
            PEMethodSymbol getMethod,
            MetadataDecoder.ParamInfo[] getMethodParams,
            PEMethodSymbol setMethod,
            MetadataDecoder.ParamInfo[] setMethodParams)
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
            MetadataDecoder.ParamInfo[] propertyParams,
            MetadataDecoder.ParamInfo[] accessorParams,
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
                parameters[ordinal] = new PEParameterSymbol(moduleSymbol, property, ordinal, paramHandle, propertyParam, out isBad);

                if (isBad)
                {
                    anyParameterIsBad = true;
                }
            }
            return parameters.AsImmutableOrNull();
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, containingType.ContainingPEModule, preferredCulture, cancellationToken, ref lazyDocComment);
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref lazyObsoleteAttributeData, this.handle, (PEModuleSymbol)(this.ContainingModule));
                return lazyObsoleteAttributeData;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return (flags & Flags.IsRuntimeSpecialName) != 0;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
