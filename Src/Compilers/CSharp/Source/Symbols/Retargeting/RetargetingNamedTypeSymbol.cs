// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly RetargetingModuleSymbol retargetingModule;

        /// <summary>
        /// The underlying NamedTypeSymbol, cannot be another RetargetingNamedTypeSymbol.
        /// </summary>
        private readonly NamedTypeSymbol underlyingType;

        private ImmutableArray<TypeParameterSymbol> lazyTypeParameters;

        private NamedTypeSymbol lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        private NamedTypeSymbol lazyDeclaredBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> lazyDeclaredInterfaces;

        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        public RetargetingNamedTypeSymbol(RetargetingModuleSymbol retargetingModule, NamedTypeSymbol underlyingType)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingType != null);
            Debug.Assert(!(underlyingType is RetargetingNamedTypeSymbol));

            this.retargetingModule = retargetingModule;
            this.underlyingType = underlyingType;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return retargetingModule.RetargetingTranslator;
            }
        }

        public NamedTypeSymbol UnderlyingNamedType
        {
            get
            {
                return this.underlyingType;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingType.IsImplicitlyDeclared; }
        }

        public override int Arity
        {
            get
            {
                return this.underlyingType.Arity;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (lazyTypeParameters.IsDefault)
                {
                    if (this.Arity == 0)
                    {
                        lazyTypeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedCompareExchange(ref lazyTypeParameters,
                            this.RetargetingTranslator.Retarget(this.underlyingType.TypeParameters), default(ImmutableArray<TypeParameterSymbol>));
                    }
                }

                return lazyTypeParameters;
            }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                // This is always the instance type, so the type arguments are the same as the type parameters.
                if (Arity > 0)
                {
                    return StaticCast<TypeSymbol>.From(this.TypeParameters);
                }
                else
                {
                    return ImmutableArray<TypeSymbol>.Empty;
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
                var underlying = this.underlyingType.EnumUnderlyingType;
                return (object)underlying == null ? null : this.RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByTypeCode); // comes from field's signature.
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return this.underlyingType.MightContainExtensionMethods;
            }
        }

        public override string Name
        {
            get
            {
                return this.underlyingType.Name;
            }
        }

        public override string MetadataName
        {
            get
            {
                return this.underlyingType.MetadataName;
            }
        }
        internal override bool HasSpecialName
        {
            get
            {
                return this.underlyingType.HasSpecialName;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return this.underlyingType.MangleName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.underlyingType.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return this.underlyingType.MemberNames;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetMembers());
        }

        internal override ImmutableArray<Symbol> GetMembersUnordered()
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetMembersUnordered());
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetMembers(name));
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            foreach (FieldSymbol f in this.underlyingType.GetFieldsToEmit())
            {
                yield return this.RetargetingTranslator.Retarget(f);
            }
        }

        internal override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            bool isInterface = this.underlyingType.IsInterfaceType();

            foreach (MethodSymbol method in this.underlyingType.GetMethodsToEmit())
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
            foreach (PropertySymbol p in this.underlyingType.GetPropertiesToEmit())
            {
                yield return this.RetargetingTranslator.Retarget(p);
            }
        }

        internal override IEnumerable<EventSymbol> GetEventsToEmit()
        {
            foreach (EventSymbol e in this.underlyingType.GetEventsToEmit())
            {
                yield return this.RetargetingTranslator.Retarget(e);
            }
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetEarlyAttributeDecodingMembers());
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetEarlyAttributeDecodingMembers(name));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetTypeMembersUnordered());
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetTypeMembers());
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetTypeMembers(name));
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetTypeMembers(name, arity));
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.underlyingType.DeclaredAccessibility;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return this.underlyingType.TypeKind;
            }
        }

        internal override bool IsInterface
        {
            get
            {
                return this.underlyingType.IsInterface;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingType.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.underlyingType.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.underlyingType.DeclaringSyntaxReferences;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return this.underlyingType.IsStatic;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return this.underlyingType.IsAbstract;
            }
        }

        internal override bool IsMetadataAbstract
        {
            get
            {
                return this.underlyingType.IsMetadataAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return this.underlyingType.IsSealed;
            }
        }

        internal override bool IsMetadataSealed
        {
            get
            {
                return this.underlyingType.IsMetadataSealed;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(this.underlyingType.GetAttributes(), ref this.lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(this.underlyingType.GetCustomAttributesToEmit(compilationState));
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        internal override NamedTypeSymbol LookupMetadataType(ref MetadataTypeName typeName)
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.LookupMetadataType(ref typeName), RetargetOptions.RetargetPrimitiveTypesByName);
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
                if (ReferenceEquals(lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    NamedTypeSymbol acyclicBase = GetDeclaredBaseType(null);
                    if (BaseTypeAnalysis.ClassDependsOn(acyclicBase, this))
                    {
                        return CyclicInheritanceError(this, acyclicBase);
                    }

                    if ((object)acyclicBase == null)
                    {
                        // if base was not declared, get it from BaseType that should set it to some default
                        var underlyingBase = underlyingType.BaseTypeNoUseSiteDiagnostics;
                        if ((object)underlyingBase != null)
                        {
                            acyclicBase = this.RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName);
                        }
                    }

                    Interlocked.CompareExchange(ref lazyBaseType, acyclicBase, ErrorTypeSymbol.UnknownResultType);
                }

                return lazyBaseType;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get
            {
                if (lazyInterfaces.IsDefault)
                {
                    var declaredInterfaces = GetDeclaredInterfaces(null);
                    if (!IsInterface)
                    {
                        // only interfaces needs to check for inheritance cycles via interfaces.
                        return declaredInterfaces;
                    }

                    ImmutableArray<NamedTypeSymbol> result = declaredInterfaces
                        .SelectAsArray(t => BaseTypeAnalysis.InterfaceDependsOn(t, this) ? CyclicInheritanceError(this, t) : t);

                    ImmutableInterlocked.InterlockedCompareExchange(ref lazyInterfaces, result, default(ImmutableArray<NamedTypeSymbol>));
                }

                return lazyInterfaces;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return this.RetargetingTranslator.Retarget(this.underlyingType.GetInterfacesToEmit());
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            if (ReferenceEquals(lazyDeclaredBaseType, ErrorTypeSymbol.UnknownResultType))
            {
                var underlyingBase = this.underlyingType.GetDeclaredBaseType(basesBeingResolved);
                var declaredBase = (object)underlyingBase != null ? this.RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName) : null;
                Interlocked.CompareExchange(ref lazyDeclaredBaseType, declaredBase, ErrorTypeSymbol.UnknownResultType);
            }

            return lazyDeclaredBaseType;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            if (lazyDeclaredInterfaces.IsDefault)
            {
                var underlyingBaseInterfaces = this.underlyingType.GetDeclaredInterfaces(basesBeingResolved);
                var result = this.RetargetingTranslator.Retarget(underlyingBaseInterfaces);
                ImmutableInterlocked.InterlockedCompareExchange(ref lazyDeclaredInterfaces, result, default(ImmutableArray<NamedTypeSymbol>));
            }

            return lazyDeclaredInterfaces;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                lazyUseSiteDiagnostic = CalculateUseSiteDiagnostic();
            }

            return lazyUseSiteDiagnostic;
        }

        internal override NamedTypeSymbol ComImportCoClass
        {
            get
            {
                NamedTypeSymbol coClass = this.underlyingType.ComImportCoClass;
                return (object)coClass == null ? null : this.RetargetingTranslator.Retarget(coClass, RetargetOptions.RetargetPrimitiveTypesByName);
            }
        }

        internal override bool IsComImport
        {
            get { return this.underlyingType.IsComImport; }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return this.underlyingType.ObsoleteAttributeData; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return this.underlyingType.ShouldAddWinRTMembers; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get { return this.underlyingType.IsWindowsRuntimeImport; }
        }

        internal override TypeLayout Layout
        {
            get { return this.underlyingType.Layout; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return this.underlyingType.MarshallingCharSet; }
        }

        internal override bool IsSerializable
        {
            get { return this.underlyingType.IsSerializable; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return this.underlyingType.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return this.underlyingType.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return this.underlyingType.GetAppliedConditionalSymbols();
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return this.underlyingType.GetAttributeUsageInfo();
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool GetGuidString(out string guidString)
        {
            return this.underlyingType.GetGuidString(out guidString);
        }
    }
}