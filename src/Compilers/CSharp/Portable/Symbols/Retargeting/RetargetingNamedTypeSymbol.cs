// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal sealed class RetargetingNamedTypeSymbol : WrappedNamedTypeSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        private NamedTypeSymbol _lazyDeclaredBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyDeclaredInterfaces;

        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private CachedUseSiteInfo<AssemblySymbol> _lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;

        public RetargetingNamedTypeSymbol(RetargetingModuleSymbol retargetingModule, NamedTypeSymbol underlyingType, TupleExtraData tupleData = null)
            : base(underlyingType, tupleData)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert(!(underlyingType is RetargetingNamedTypeSymbol));

            _retargetingModule = retargetingModule;
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            return new RetargetingNamedTypeSymbol(_retargetingModule, _underlyingType, newData);
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
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

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                // This is always the instance type, so the type arguments are the same as the type parameters.
                return GetTypeParametersAsTypeArguments();
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

        public override IEnumerable<string> MemberNames
        {
            get
            {
                return _underlyingType.MemberNames;
            }
        }

        internal override bool HasDeclaredRequiredMembers => _underlyingType.HasDeclaredRequiredMembers;

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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers(name));
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetTypeMembers(name, arity));
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingType.ContainingSymbol);
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingType.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingType.GetCustomAttributesToEmit(moduleBuilder));
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

#nullable enable

        internal override NamedTypeSymbol? LookupMetadataType(ref MetadataTypeName typeName)
        {
            NamedTypeSymbol? underlyingResult = _underlyingType.LookupMetadataType(ref typeName);

            if (underlyingResult is null)
            {
                return null;
            }

            Debug.Assert(!underlyingResult.IsErrorType());
            Debug.Assert((object)_underlyingType == underlyingResult.ContainingSymbol);

            return this.RetargetingTranslator.Retarget(underlyingResult, RetargetOptions.RetargetPrimitiveTypesByName);
        }

#nullable disable

        private static ExtendedErrorTypeSymbol CyclicInheritanceError(TypeSymbol declaredBase)
        {
            var info = new CSDiagnosticInfo(ErrorCode.ERR_ImportedCircularBase, declaredBase);
            return new ExtendedErrorTypeSymbol(declaredBase, LookupResultKind.NotReferencable, info, true);
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    NamedTypeSymbol acyclicBase = GetDeclaredBaseType(null);

                    if ((object)acyclicBase == null)
                    {
                        // if base was not declared, get it from BaseType that should set it to some default
                        var underlyingBase = _underlyingType.BaseTypeNoUseSiteDiagnostics;
                        if ((object)underlyingBase != null)
                        {
                            acyclicBase = this.RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName);
                        }
                    }

                    if ((object)acyclicBase != null && BaseTypeAnalysis.TypeDependsOn(acyclicBase, this))
                    {
                        return CyclicInheritanceError(acyclicBase);
                    }

                    Interlocked.CompareExchange(ref _lazyBaseType, acyclicBase, ErrorTypeSymbol.UnknownResultType);
                }

                return _lazyBaseType;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
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
                    .SelectAsArray(t => BaseTypeAnalysis.TypeDependsOn(t, this) ? CyclicInheritanceError(t) : t);

                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, result, default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyInterfaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return this.RetargetingTranslator.Retarget(_underlyingType.GetInterfacesToEmit());
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            if (ReferenceEquals(_lazyDeclaredBaseType, ErrorTypeSymbol.UnknownResultType))
            {
                var underlyingBase = _underlyingType.GetDeclaredBaseType(basesBeingResolved);
                var declaredBase = (object)underlyingBase != null ? this.RetargetingTranslator.Retarget(underlyingBase, RetargetOptions.RetargetPrimitiveTypesByName) : null;
                Interlocked.CompareExchange(ref _lazyDeclaredBaseType, declaredBase, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyDeclaredBaseType;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            if (_lazyDeclaredInterfaces.IsDefault)
            {
                var underlyingBaseInterfaces = _underlyingType.GetDeclaredInterfaces(basesBeingResolved);
                var result = this.RetargetingTranslator.Retarget(underlyingBaseInterfaces);
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyDeclaredInterfaces, result, default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyDeclaredInterfaces;
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (!_lazyCachedUseSiteInfo.IsInitialized)
            {
                AssemblySymbol primaryDependency = PrimaryDependency;
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, new UseSiteInfo<AssemblySymbol>(primaryDependency).AdjustDiagnosticInfo(CalculateUseSiteDiagnostic()));
            }

            return _lazyCachedUseSiteInfo.ToUseSiteInfo(PrimaryDependency);
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

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        public sealed override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool IsFileLocal => _underlyingType.IsFileLocal;
        internal override FileIdentifier? AssociatedFileIdentifier => _underlyingType.AssociatedFileIdentifier;

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;

        internal sealed override bool IsRecord => _underlyingType.IsRecord;
        internal sealed override bool IsRecordStruct => _underlyingType.IsRecordStruct;
        internal sealed override bool HasPossibleWellKnownCloneMethod() => _underlyingType.HasPossibleWellKnownCloneMethod();

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            foreach ((MethodSymbol body, MethodSymbol implemented) in _underlyingType.SynthesizedInterfaceMethodImpls())
            {
                var newBody = this.RetargetingTranslator.Retarget(body, MemberSignatureComparer.RetargetedExplicitImplementationComparer);
                var newImplemented = this.RetargetingTranslator.Retarget(implemented, MemberSignatureComparer.RetargetedExplicitImplementationComparer);

                if (newBody is object && newImplemented is object)
                {
                    yield return (newBody, newImplemented);
                }
            }
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            return _underlyingType.HasInlineArrayAttribute(out length);
        }
    }
}
