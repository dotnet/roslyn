// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a type of a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another NamedTypeSymbol that is responsible for retargeting referenced symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingNamedTypeSymbol : NamedTypeSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying NamedTypeSymbol, cannot be another RetargetingNamedTypeSymbol.
        /// </summary>
        private readonly NamedTypeSymbol _underlyingType;

        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        private NamedTypeSymbol _lazyDeclaredBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyDeclaredInterfaces;

        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingNamedTypeSymbol(RetargetingModuleSymbol retargetingModule, NamedTypeSymbol underlyingType)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingType != null);
            Debug.Assert(!(underlyingType is RetargetingNamedTypeSymbol));

            _retargetingModule = retargetingModule;
            _underlyingType = underlyingType;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public NamedTypeSymbol UnderlyingNamedType
        {
            get
            {
                return _underlyingType;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingType.IsImplicitlyDeclared; }
        }

        public override int Arity
        {
            get
            {
                return _underlyingType.Arity;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (_lazyTypeParameters.IsDefault)
                {
                    if (this.Arity == 0)
                    {
                        _lazyTypeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters,
                            this.RetargetingTranslator.Retarget(_underlyingType.TypeParameters), default(ImmutableArray<TypeParameterSymbol>));
                    }
                }

                return _lazyTypeParameters;
            }
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                // This is always the instance type, so the type arguments are the same as the type parameters.
                if (Arity > 0)
                {
                    return this.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations);
                }
                else
                {
                    return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
                }
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                var underlying = _underlyingType.EnumUnderlyingType;
                return (object)underlying == null ? null : this.RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByTypeCode); // comes from field's signature.
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return _underlyingType.MightContainExtensionMethods;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingType.Name;
            }
        }

        public override string MetadataName
        {
            get
            {
                return _underlyingType.MetadataName;
            }
        }
        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingType.HasSpecialName;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return _underlyingType.MangleName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingType.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return _underlyingType.MemberNames;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetMembers());
        }

        internal override ImmutableArray<Symbol> GetMembersUnordered()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetMembersUnordered());
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetMembers(name));
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            foreach (FieldSymbol f in _underlyingType.GetFieldsToEmit())
            {
                yield return this.RetargetingTranslator.Retarget(f);
            }
        }

        internal override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            bool isInterface = _underlyingType.IsInterfaceType();

            foreach (MethodSymbol method in _underlyingType.GetMethodsToEmit())
            {
                Debug.Assert((object)method != null);

                int gapSize = isInterface ? Microsoft.CodeAnalysis.ModuleExtensions.GetVTableGapSize(method.MetadataName) : 0;
                if (gapSize > 0)
                {
                    do
                    {
                        yield return null;
                        gapSize--;
                    }
                    while (gapSize > 0);
                }
                else
                {
                    yield return this.RetargetingTranslator.Retarget(method);
                }
            }
        }

        internal override IEnumerable<PropertySymbol> GetPropertiesToEmit()
        {
            foreach (PropertySymbol p in _underlyingType.GetPropertiesToEmit())
            {
                yield return this.RetargetingTranslator.Retarget(p);
            }
        }

        internal override IEnumerable<EventSymbol> GetEventsToEmit()
        {
            foreach (EventSymbol e in _underlyingType.GetEventsToEmit())
            {
                yield return this.RetargetingTranslator.Retarget(e);
            }
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetEarlyAttributeDecodingMembers());
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetEarlyAttributeDecodingMembers(name));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetTypeMembersUnordered());
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers());
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers(name));
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers(name, arity));
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingType.DeclaredAccessibility;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return _underlyingType.TypeKind;
            }
        }

        internal override bool IsInterface
        {
            get
            {
                return _underlyingType.IsInterface;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingType.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingType.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingType.DeclaringSyntaxReferences;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingType.IsStatic;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingType.IsAbstract;
            }
        }

        internal override bool IsMetadataAbstract
        {
            get
            {
                return _underlyingType.IsMetadataAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingType.IsSealed;
            }
        }

        internal override bool IsMetadataSealed
        {
            get
            {
                return _underlyingType.IsMetadataSealed;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingType.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingType.GetCustomAttributesToEmit(compilationState));
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        internal override NamedTypeSymbol LookupMetadataType(ref MetadataTypeName typeName)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.LookupMetadataType(ref typeName), RetargetOptions.RetargetPrimitiveTypesByName);
        }

        private static ExtendedErrorTypeSymbol CyclicInheritanceError(RetargetingNamedTypeSymbol type, TypeSymbol declaredBase)
        {
            var info = new CSDiagnosticInfo(ErrorCode.ERR_ImportedCircularBase, declaredBase, type);
            return new ExtendedErrorTypeSymbol(declaredBase, LookupResultKind.NotReferencable, info, true);
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    NamedTypeSymbol acyclicBase = GetDeclaredBaseType(null);
                    if (BaseTypeAnalysis.ClassDependsOn(acyclicBase, this))
                    {
                        return CyclicInheritanceError(this, acyclicBase);
                    }

                    if ((object)acyclicBase == null)
                    {
                        // if base was not declared, get it from BaseType that should set it to some default
                        var underlyingBase = _underlyingType.BaseTypeNoUseSiteDiagnostics;
                        if ((object)underlyingBase != null)
                        {
                            acyclicBase = this.RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName);
                        }
                    }

                    Interlocked.CompareExchange(ref _lazyBaseType, acyclicBase, ErrorTypeSymbol.UnknownResultType);
                }

                return _lazyBaseType;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            if (_lazyInterfaces.IsDefault)
            {
                var declaredInterfaces = GetDeclaredInterfaces(basesBeingResolved);
                if (!IsInterface)
                {
                    // only interfaces needs to check for inheritance cycles via interfaces.
                    return declaredInterfaces;
                }

                ImmutableArray<NamedTypeSymbol> result = declaredInterfaces
                    .SelectAsArray(t => BaseTypeAnalysis.InterfaceDependsOn(t, this) ? CyclicInheritanceError(this, t) : t);

                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, result, default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyInterfaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetInterfacesToEmit());
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            if (ReferenceEquals(_lazyDeclaredBaseType, ErrorTypeSymbol.UnknownResultType))
            {
                var underlyingBase = _underlyingType.GetDeclaredBaseType(basesBeingResolved);
                var declaredBase = (object)underlyingBase != null ? this.RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName) : null;
                Interlocked.CompareExchange(ref _lazyDeclaredBaseType, declaredBase, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyDeclaredBaseType;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            if (_lazyDeclaredInterfaces.IsDefault)
            {
                var underlyingBaseInterfaces = _underlyingType.GetDeclaredInterfaces(basesBeingResolved);
                var result = this.RetargetingTranslator.Retarget(underlyingBaseInterfaces);
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyDeclaredInterfaces, result, default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyDeclaredInterfaces;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                _lazyUseSiteDiagnostic = CalculateUseSiteDiagnostic();
            }

            return _lazyUseSiteDiagnostic;
        }

        internal override NamedTypeSymbol ComImportCoClass
        {
            get
            {
                NamedTypeSymbol coClass = _underlyingType.ComImportCoClass;
                return (object)coClass == null ? null : this.RetargetingTranslator.Retarget(coClass, RetargetOptions.RetargetPrimitiveTypesByName);
            }
        }

        internal override bool IsComImport
        {
            get { return _underlyingType.IsComImport; }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _underlyingType.ObsoleteAttributeData; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return _underlyingType.ShouldAddWinRTMembers; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get { return _underlyingType.IsWindowsRuntimeImport; }
        }

        internal override TypeLayout Layout
        {
            get { return _underlyingType.Layout; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return _underlyingType.MarshallingCharSet; }
        }

        internal override bool IsSerializable
        {
            get { return _underlyingType.IsSerializable; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return _underlyingType.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return _underlyingType.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _underlyingType.GetAppliedConditionalSymbols();
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return _underlyingType.GetAttributeUsageInfo();
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool GetGuidString(out string guidString)
        {
            return _underlyingType.GetGuidString(out guidString);
        }
    }
}
