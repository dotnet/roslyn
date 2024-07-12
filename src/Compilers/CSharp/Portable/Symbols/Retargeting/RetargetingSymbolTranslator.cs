// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    internal enum RetargetOptions : byte
    {
        RetargetPrimitiveTypesByName = 0,
        RetargetPrimitiveTypesByTypeCode = 1,
    }

    internal partial class RetargetingModuleSymbol
    {
        /// <summary>
        /// Retargeting map from underlying module to this one.
        /// </summary>
        private readonly ConcurrentDictionary<Symbol, Symbol> _symbolMap =
            new ConcurrentDictionary<Symbol, Symbol>(concurrencyLevel: 2, capacity: 4);

        private readonly Func<Symbol, RetargetingMethodSymbol> _createRetargetingMethod;
        private readonly Func<Symbol, RetargetingNamespaceSymbol> _createRetargetingNamespace;
        private readonly Func<Symbol, RetargetingTypeParameterSymbol> _createRetargetingTypeParameter;
        private readonly Func<Symbol, RetargetingNamedTypeSymbol> _createRetargetingNamedType;
        private readonly Func<Symbol, FieldSymbol> _createRetargetingField;
        private readonly Func<Symbol, RetargetingPropertySymbol> _createRetargetingProperty;
        private readonly Func<Symbol, RetargetingEventSymbol> _createRetargetingEvent;

        private RetargetingMethodSymbol CreateRetargetingMethod(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            return new RetargetingMethodSymbol(this, (MethodSymbol)symbol);
        }

        private RetargetingNamespaceSymbol CreateRetargetingNamespace(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            return new RetargetingNamespaceSymbol(this, (NamespaceSymbol)symbol);
        }

        private RetargetingNamedTypeSymbol CreateRetargetingNamedType(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            return new RetargetingNamedTypeSymbol(this, (NamedTypeSymbol)symbol);
        }

        private FieldSymbol CreateRetargetingField(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            if (symbol is TupleErrorFieldSymbol tupleErrorField)
            {
                var correspondingTupleField = tupleErrorField.CorrespondingTupleField;
                Debug.Assert(correspondingTupleField is TupleErrorFieldSymbol);

                var retargetedCorrespondingDefaultFieldOpt = (correspondingTupleField == (object)tupleErrorField)
                    ? null
                    : (TupleErrorFieldSymbol)RetargetingTranslator.Retarget(correspondingTupleField);

                return new TupleErrorFieldSymbol(
                    RetargetingTranslator.Retarget(tupleErrorField.ContainingType, RetargetOptions.RetargetPrimitiveTypesByName),
                    tupleErrorField.Name,
                    tupleErrorField.TupleElementIndex,
                    tupleErrorField.TryGetFirstLocation(),
                    this.RetargetingTranslator.Retarget(tupleErrorField.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                    tupleErrorField.GetUseSiteInfo().DiagnosticInfo,
                    tupleErrorField.IsImplicitlyDeclared,
                    retargetedCorrespondingDefaultFieldOpt);
            }

            return new RetargetingFieldSymbol(this, (FieldSymbol)symbol);
        }

        private RetargetingPropertySymbol CreateRetargetingProperty(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            return new RetargetingPropertySymbol(this, (PropertySymbol)symbol);
        }

        private RetargetingEventSymbol CreateRetargetingEvent(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            return new RetargetingEventSymbol(this, (EventSymbol)symbol);
        }

        private RetargetingTypeParameterSymbol CreateRetargetingTypeParameter(Symbol symbol)
        {
            Debug.Assert(ReferenceEquals(symbol.ContainingModule, _underlyingModule));
            return new RetargetingTypeParameterSymbol(this, (TypeParameterSymbol)symbol);
        }

        internal class RetargetingSymbolTranslator
            : CSharpSymbolVisitor<RetargetOptions, Symbol>
        {
            private readonly RetargetingModuleSymbol _retargetingModule;

            public RetargetingSymbolTranslator(RetargetingModuleSymbol retargetingModule)
            {
                Debug.Assert((object)retargetingModule != null);
                _retargetingModule = retargetingModule;
            }

            /// <summary>
            /// Retargeting map from underlying module to the retargeting module.
            /// </summary>
            private ConcurrentDictionary<Symbol, Symbol> SymbolMap
            {
                get
                {
                    return _retargetingModule._symbolMap;
                }
            }

            /// <summary>
            /// RetargetingAssemblySymbol owning retargetingModule.
            /// </summary>
            private RetargetingAssemblySymbol RetargetingAssembly
            {
                get
                {
                    return _retargetingModule._retargetingAssembly;
                }
            }

            /// <summary>
            /// The underlying ModuleSymbol for retargetingModule.
            /// </summary>
            private SourceModuleSymbol UnderlyingModule
            {
                get
                {
                    return _retargetingModule._underlyingModule;
                }
            }

            /// <summary>
            /// The map that captures information about what assembly should be retargeted 
            /// to what assembly. Key is the AssemblySymbol referenced by the underlying module,
            /// value is the corresponding AssemblySymbol referenced by the retargeting module, and 
            /// corresponding retargeting map for symbols.
            /// </summary>
            private Dictionary<AssemblySymbol, DestinationData> RetargetingAssemblyMap
            {
                get
                {
                    return _retargetingModule._retargetingAssemblyMap;
                }
            }

            public Symbol Retarget(Symbol symbol)
            {
                Debug.Assert(symbol.Kind != SymbolKind.NamedType || ((NamedTypeSymbol)symbol).PrimitiveTypeCode == Cci.PrimitiveTypeCode.NotPrimitive);
                return symbol.Accept(this, RetargetOptions.RetargetPrimitiveTypesByName);
            }

            public MarshalPseudoCustomAttributeData Retarget(MarshalPseudoCustomAttributeData marshallingInfo)
            {
                // Retarget by type code - primitive types are encoded in short form in an attribute signature:
                return marshallingInfo?.WithTranslatedTypes<TypeSymbol, RetargetingSymbolTranslator>(
                    translator: (type, translator) => translator.Retarget(type, RetargetOptions.RetargetPrimitiveTypesByTypeCode), arg: this);
            }

            public TypeSymbol Retarget(TypeSymbol symbol, RetargetOptions options)
            {
                return (TypeSymbol)symbol.Accept(this, options);
            }

            public TypeWithAnnotations Retarget(TypeWithAnnotations underlyingType, RetargetOptions options, NamedTypeSymbol asDynamicIfNoPiaContainingType = null)
            {
                var newTypeSymbol = Retarget(underlyingType.Type, options);

                if ((object)asDynamicIfNoPiaContainingType != null)
                {
                    newTypeSymbol = newTypeSymbol.AsDynamicIfNoPia(asDynamicIfNoPiaContainingType);
                }

                bool modifiersHaveChanged;
                var newModifiers = RetargetModifiers(underlyingType.CustomModifiers, out modifiersHaveChanged);

                if (modifiersHaveChanged || !TypeSymbol.Equals(underlyingType.Type, newTypeSymbol, TypeCompareKind.ConsiderEverything2))
                {
                    return underlyingType.WithTypeAndModifiers(newTypeSymbol, newModifiers);
                }

                return underlyingType;
            }

            public NamespaceSymbol Retarget(NamespaceSymbol ns)
            {
                return (NamespaceSymbol)this.SymbolMap.GetOrAdd(ns, _retargetingModule._createRetargetingNamespace);
            }

            private NamedTypeSymbol RetargetNamedTypeDefinition(NamedTypeSymbol type, RetargetOptions options)
            {
                Debug.Assert(type.IsDefinition);

                if (type.IsNativeIntegerWrapperType)
                {
                    var result = RetargetNamedTypeDefinition(type.NativeIntegerUnderlyingType, options);
                    return result.SpecialType == SpecialType.None ? result : result.AsNativeInteger();
                }

                // Before we do anything else, check if we need to do special retargeting
                // for primitive type references encoded with enum values in metadata signatures.
                if (options == RetargetOptions.RetargetPrimitiveTypesByTypeCode)
                {
                    Cci.PrimitiveTypeCode typeCode = type.PrimitiveTypeCode;

                    if (typeCode != Cci.PrimitiveTypeCode.NotPrimitive)
                    {
                        return RetargetingAssembly.GetPrimitiveType(typeCode);
                    }
                }

                if (type.Kind == SymbolKind.ErrorType)
                {
                    return Retarget((ErrorTypeSymbol)type);
                }

                AssemblySymbol retargetFrom = type.ContainingAssembly;

                // Deal with "to be local" NoPia types leaking through source module.
                // These are the types that are coming from assemblies linked (/l-ed) 
                // by the compilation that created the source module.
                bool isLocalType;

                if (ReferenceEquals(retargetFrom, this.RetargetingAssembly.UnderlyingAssembly))
                {
                    Debug.Assert(!retargetFrom.IsLinked);
                    isLocalType = type.IsExplicitDefinitionOfNoPiaLocalType;
                }
                else
                {
                    isLocalType = retargetFrom.IsLinked;
                }

                if (isLocalType)
                {
                    return RetargetNoPiaLocalType(type);
                }

                // Perform general retargeting.

                if (ReferenceEquals(retargetFrom, this.RetargetingAssembly.UnderlyingAssembly))
                {
                    return RetargetNamedTypeDefinitionFromUnderlyingAssembly(type);
                }

                // Does this type come from one of the retargeted assemblies?
                DestinationData destination;

                if (!this.RetargetingAssemblyMap.TryGetValue(retargetFrom, out destination))
                {
                    // No need to retarget
                    return type;
                }

                // Retarget from one assembly to another
                type = PerformTypeRetargeting(ref destination, type);
                this.RetargetingAssemblyMap[retargetFrom] = destination;
                return type;
            }

            private NamedTypeSymbol RetargetNamedTypeDefinitionFromUnderlyingAssembly(NamedTypeSymbol type)
            {
                // The type is defined in the underlying assembly.
                var module = type.ContainingModule;

                if (ReferenceEquals(module, this.UnderlyingModule))
                {
                    Debug.Assert(module.Ordinal == 0);
                    Debug.Assert(!type.IsExplicitDefinitionOfNoPiaLocalType);
                    var container = type.ContainingType;

                    while ((object)container != null)
                    {
                        if (container.IsExplicitDefinitionOfNoPiaLocalType)
                        {
                            // Types nested into local types are not supported.
                            return (NamedTypeSymbol)this.SymbolMap.GetOrAdd(type, new UnsupportedMetadataTypeSymbol());
                        }

                        container = container.ContainingType;
                    }

                    return (NamedTypeSymbol)this.SymbolMap.GetOrAdd(type, _retargetingModule._createRetargetingNamedType);
                }
                else
                {
                    // The type is defined in one of the added modules
                    Debug.Assert(module.Ordinal > 0);
                    PEModuleSymbol addedModule = (PEModuleSymbol)this.RetargetingAssembly.Modules[module.Ordinal];
                    Debug.Assert(ReferenceEquals(((PEModuleSymbol)module).Module, addedModule.Module));
                    return RetargetNamedTypeDefinition((PENamedTypeSymbol)type, addedModule);
                }
            }

            private NamedTypeSymbol RetargetNoPiaLocalType(NamedTypeSymbol type)
            {
                NamedTypeSymbol cached;

                var map = this.RetargetingAssembly.NoPiaUnificationMap;
                if (map.TryGetValue(type, out cached))
                {
                    return cached;
                }

                NamedTypeSymbol result;

                if (type.ContainingSymbol.Kind != SymbolKind.NamedType &&
                    type.Arity == 0)
                {
                    // Get type's identity

                    bool isInterface = type.IsInterface;
                    bool hasGuid = false;
                    string interfaceGuid = null;
                    string scope = null;

                    if (isInterface)
                    {
                        // Get type's Guid
                        hasGuid = type.GetGuidString(out interfaceGuid);
                    }

                    MetadataTypeName name = MetadataTypeName.FromFullName(type.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat), forcedArity: type.Arity);
                    string identifier = null;

                    if ((object)type.ContainingModule == (object)_retargetingModule.UnderlyingModule)
                    {
                        // This is a local type explicitly declared in source. Get information from TypeIdentifier attribute.
                        foreach (var attrData in type.GetAttributes())
                        {
                            int signatureIndex = attrData.GetTargetAttributeSignatureIndex(AttributeDescription.TypeIdentifierAttribute);

                            if (signatureIndex != -1)
                            {
                                Debug.Assert(signatureIndex == 0 || signatureIndex == 1);

                                if (signatureIndex == 1 && attrData.CommonConstructorArguments.Length == 2)
                                {
                                    scope = attrData.CommonConstructorArguments[0].ValueInternal as string;
                                    identifier = attrData.CommonConstructorArguments[1].ValueInternal as string;
                                }

                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert((object)type.ContainingAssembly != (object)RetargetingAssembly.UnderlyingAssembly);

                        // Note, this logic should match the one in EmbeddedType.Cci.IReference.GetAttributes.
                        // Here we are trying to predict what attributes we will emit on embedded type, which corresponds the 
                        // type we are retargeting. That function actually emits the attributes.

                        if (!(hasGuid && isInterface))
                        {
                            type.ContainingAssembly.GetGuidString(out scope);
                            identifier = name.FullName;
                        }
                    }

                    result = MetadataDecoder.SubstituteNoPiaLocalType(
                        ref name,
                        isInterface,
                        type.BaseTypeNoUseSiteDiagnostics,
                        interfaceGuid,
                        scope,
                        identifier,
                        RetargetingAssembly);

                    Debug.Assert((object)result != null);
                }
                else
                {
                    // TODO: report better error?
                    result = new UnsupportedMetadataTypeSymbol();
                }

                cached = map.GetOrAdd(type, result);

                return cached;
            }

#nullable enable

            private static NamedTypeSymbol RetargetNamedTypeDefinition(PENamedTypeSymbol type, PEModuleSymbol addedModule)
            {
                Debug.Assert(!type.ContainingModule.Equals(addedModule) &&
                             ReferenceEquals(((PEModuleSymbol)type.ContainingModule).Module, addedModule.Module));

                TypeSymbol? cached;

                if (addedModule.TypeHandleToTypeMap.TryGetValue(type.Handle, out cached))
                {
                    return (NamedTypeSymbol)cached;
                }

                NamedTypeSymbol? result;

                NamedTypeSymbol containingType = type.ContainingType;
                MetadataTypeName mdName;

                if ((object)containingType != null)
                {
                    // Nested type.  We need to retarget 
                    // the enclosing type and then go back and get the type we are interested in.

                    NamedTypeSymbol scope = RetargetNamedTypeDefinition((PENamedTypeSymbol)containingType, addedModule);

                    mdName = MetadataTypeName.FromTypeName(type.MetadataName, forcedArity: type.Arity);
                    result = scope.LookupMetadataType(ref mdName);
                }
                else
                {
                    string namespaceName = type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
                    mdName = MetadataTypeName.FromNamespaceAndTypeName(namespaceName, type.MetadataName, forcedArity: type.Arity);
                    result = addedModule.LookupTopLevelMetadataType(ref mdName);
                }

                Debug.Assert(result is PENamedTypeSymbol peResult && peResult.Handle == type.Handle);

                return result;
            }

            private static NamedTypeSymbol PerformTypeRetargeting(
                ref DestinationData destination,
                NamedTypeSymbol type)
            {
                NamedTypeSymbol? result;

                if (!destination.SymbolMap.TryGetValue(type, out result))
                {
                    // Lookup by name as a TypeRef.
                    NamedTypeSymbol containingType = type.ContainingType;
                    NamedTypeSymbol? result1;
                    MetadataTypeName mdName;

                    if ((object)containingType != null)
                    {
                        // This happens if type is a nested class.  We need to retarget 
                        // the enclosing class and then go back and get the type we are interested in.

                        NamedTypeSymbol scope = PerformTypeRetargeting(ref destination, containingType);
                        mdName = MetadataTypeName.FromTypeName(type.MetadataName, forcedArity: type.Arity);
                        result1 = scope.LookupMetadataType(ref mdName);
                        Debug.Assert(result1?.IsErrorType() != true);

                        result1 ??= new MissingMetadataTypeSymbol.Nested(scope, ref mdName);
                    }
                    else
                    {
                        string namespaceName = type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
                        mdName = MetadataTypeName.FromNamespaceAndTypeName(namespaceName, type.MetadataName, forcedArity: type.Arity);
                        result1 = destination.To.LookupDeclaredOrForwardedTopLevelMetadataType(ref mdName, visitedAssemblies: null);
                    }

                    Debug.Assert(result1.Arity == type.Arity);

                    result = destination.SymbolMap.GetOrAdd(type, result1);
                    Debug.Assert(TypeSymbol.Equals(result1, result, TypeCompareKind.ConsiderEverything2));
                }

                return result;
            }

#nullable disable

            public NamedTypeSymbol Retarget(NamedTypeSymbol type, RetargetOptions options)
            {
                NamedTypeSymbol originalDefinition = type.OriginalDefinition;

                NamedTypeSymbol newDefinition = RetargetNamedTypeDefinition(originalDefinition, options);

                if (ReferenceEquals(type, originalDefinition))
                {
                    return newDefinition;
                }

                if (newDefinition.Kind == SymbolKind.ErrorType && !newDefinition.IsGenericType)
                {
                    return newDefinition;
                }

                Debug.Assert(originalDefinition.Arity == 0 || !ReferenceEquals(type.ConstructedFrom, type));
                if (type.IsUnboundGenericType)
                {
                    if (ReferenceEquals(newDefinition, originalDefinition))
                    {
                        return type;
                    }

                    return newDefinition.AsUnboundGenericType();
                }

                Debug.Assert((object)type.ContainingType == null || !type.ContainingType.IsUnboundGenericType());

                // This must be a generic instantiation (i.e. constructed type).

                NamedTypeSymbol genericType = type;
                var oldArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                int startOfNonInterfaceArguments = int.MaxValue;

                // Collect generic arguments for the type and its containers.
                while ((object)genericType != null)
                {
                    if (startOfNonInterfaceArguments == int.MaxValue &&
                        !genericType.IsInterface)
                    {
                        startOfNonInterfaceArguments = oldArguments.Count;
                    }

                    oldArguments.AddRange(genericType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
                    genericType = genericType.ContainingType;
                }

                bool anythingRetargeted = !originalDefinition.Equals(newDefinition);

                // retarget the arguments
                var newArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance(oldArguments.Count);

                foreach (var arg in oldArguments)
                {
                    var newArg = Retarget(arg, RetargetOptions.RetargetPrimitiveTypesByTypeCode); // generic instantiation is a signature

                    if (!anythingRetargeted && !newArg.IsSameAs(arg))
                    {
                        anythingRetargeted = true;
                    }

                    newArguments.Add(newArg);
                }

                // See if it is or its enclosing type is a non-interface closed over NoPia local types. 
                bool noPiaIllegalGenericInstantiation = IsNoPiaIllegalGenericInstantiation(oldArguments, newArguments, startOfNonInterfaceArguments);
                oldArguments.Free();
                NamedTypeSymbol constructedType;

                if (!anythingRetargeted)
                {
                    // Nothing was retargeted, return original type symbol.
                    constructedType = type;
                }
                else
                {
                    // Create symbol for new constructed type and return it.

                    // need to collect type parameters in the same order as we have arguments, 
                    // but this should be done for the new definition.
                    genericType = newDefinition;
                    ArrayBuilder<TypeParameterSymbol> newParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance(newArguments.Count);

                    // Collect generic arguments for the type and its containers.
                    while ((object)genericType != null)
                    {
                        if (genericType.Arity > 0)
                        {
                            newParameters.AddRange(genericType.TypeParameters);
                        }

                        genericType = genericType.ContainingType;
                    }

                    Debug.Assert(newParameters.Count == newArguments.Count);

                    TypeMap substitution = new TypeMap(newParameters.ToImmutableAndFree(), newArguments.ToImmutable());

                    constructedType = substitution.SubstituteNamedType(newDefinition).WithTupleDataFrom(type);
                }

                newArguments.Free();

                if (noPiaIllegalGenericInstantiation)
                {
                    return new NoPiaIllegalGenericInstantiationSymbol(_retargetingModule, constructedType);
                }

                return constructedType;
            }

            private bool IsNoPiaIllegalGenericInstantiation(ArrayBuilder<TypeWithAnnotations> oldArguments, ArrayBuilder<TypeWithAnnotations> newArguments, int startOfNonInterfaceArguments)
            {
                // TODO: Do we need to check constraints on type parameters as well?

                if (this.UnderlyingModule.ContainsExplicitDefinitionOfNoPiaLocalTypes)
                {
                    for (int i = startOfNonInterfaceArguments; i < oldArguments.Count; i++)
                    {
                        if (IsOrClosedOverAnExplicitLocalType(oldArguments[i].Type))
                        {
                            return true;
                        }
                    }
                }

                ImmutableArray<AssemblySymbol> assembliesToEmbedTypesFrom = this.UnderlyingModule.GetAssembliesToEmbedTypesFrom();

                if (assembliesToEmbedTypesFrom.Length > 0)
                {
                    for (int i = startOfNonInterfaceArguments; i < oldArguments.Count; i++)
                    {
                        if (MetadataDecoder.IsOrClosedOverATypeFromAssemblies(oldArguments[i].Type, assembliesToEmbedTypesFrom))
                        {
                            return true;
                        }
                    }
                }

                ImmutableArray<AssemblySymbol> linkedAssemblies = RetargetingAssembly.GetLinkedReferencedAssemblies();

                if (!linkedAssemblies.IsDefaultOrEmpty)
                {
                    for (int i = startOfNonInterfaceArguments; i < newArguments.Count; i++)
                    {
                        if (MetadataDecoder.IsOrClosedOverATypeFromAssemblies(newArguments[i].Type, linkedAssemblies))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Perform a check whether the type or at least one of its generic arguments 
            /// is an explicitly defined local type. The check is performed recursively. 
            /// </summary>
            private bool IsOrClosedOverAnExplicitLocalType(TypeSymbol symbol)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.TypeParameter:
                        return false;

                    case SymbolKind.ArrayType:
                        return IsOrClosedOverAnExplicitLocalType(((ArrayTypeSymbol)symbol).ElementType);

                    case SymbolKind.PointerType:
                        return IsOrClosedOverAnExplicitLocalType(((PointerTypeSymbol)symbol).PointedAtType);

                    case SymbolKind.DynamicType:
                        return false;

                    case SymbolKind.ErrorType:
                    case SymbolKind.NamedType:

                        var namedType = (NamedTypeSymbol)symbol;
                        if ((object)symbol.OriginalDefinition.ContainingModule == (object)_retargetingModule.UnderlyingModule &&
                            namedType.IsExplicitDefinitionOfNoPiaLocalType)
                        {
                            return true;
                        }

                        do
                        {
                            foreach (var argument in namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
                            {
                                if (IsOrClosedOverAnExplicitLocalType(argument.Type))
                                {
                                    return true;
                                }
                            }

                            namedType = namedType.ContainingType;
                        }
                        while ((object)namedType != null);

                        return false;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
                }
            }

            public virtual TypeParameterSymbol Retarget(TypeParameterSymbol typeParameter)
            {
                return (TypeParameterSymbol)this.SymbolMap.GetOrAdd(typeParameter, _retargetingModule._createRetargetingTypeParameter);
            }

            public ArrayTypeSymbol Retarget(ArrayTypeSymbol type)
            {
                TypeWithAnnotations oldElement = type.ElementTypeWithAnnotations;
                TypeWithAnnotations newElement = Retarget(oldElement, RetargetOptions.RetargetPrimitiveTypesByTypeCode);

                if (oldElement.IsSameAs(newElement))
                {
                    return type;
                }

                if (type.IsSZArray)
                {
                    return ArrayTypeSymbol.CreateSZArray(this.RetargetingAssembly, newElement);
                }

                return ArrayTypeSymbol.CreateMDArray(this.RetargetingAssembly, newElement, type.Rank, type.Sizes, type.LowerBounds);
            }

            internal ImmutableArray<CustomModifier> RetargetModifiers(ImmutableArray<CustomModifier> oldModifiers, out bool modifiersHaveChanged)
            {
                ArrayBuilder<CustomModifier> newModifiers = null;

                for (int i = 0; i < oldModifiers.Length; i++)
                {
                    var oldModifier = oldModifiers[i];
                    NamedTypeSymbol oldModifierSymbol = ((CSharpCustomModifier)oldModifier).ModifierSymbol;
                    NamedTypeSymbol newModifierSymbol = Retarget(oldModifierSymbol, RetargetOptions.RetargetPrimitiveTypesByName); // should be retargeted by name

                    if (!newModifierSymbol.Equals(oldModifierSymbol))
                    {
                        if (newModifiers == null)
                        {
                            newModifiers = ArrayBuilder<CustomModifier>.GetInstance(oldModifiers.Length);
                            newModifiers.AddRange(oldModifiers, i);
                        }

                        newModifiers.Add(oldModifier.IsOptional ?
                                            CSharpCustomModifier.CreateOptional(newModifierSymbol) :
                                            CSharpCustomModifier.CreateRequired(newModifierSymbol));
                    }
                    else if (newModifiers != null)
                    {
                        newModifiers.Add(oldModifier);
                    }
                }

                Debug.Assert(newModifiers == null || newModifiers.Count == oldModifiers.Length);
                modifiersHaveChanged = (newModifiers != null);
                return modifiersHaveChanged ? newModifiers.ToImmutableAndFree() : oldModifiers;
            }

            public PointerTypeSymbol Retarget(PointerTypeSymbol type)
            {
                TypeWithAnnotations oldPointed = type.PointedAtTypeWithAnnotations;
                TypeWithAnnotations newPointed = Retarget(oldPointed, RetargetOptions.RetargetPrimitiveTypesByTypeCode);

                if (oldPointed.IsSameAs(newPointed))
                {
                    return type;
                }

                return new PointerTypeSymbol(newPointed);
            }

            public FunctionPointerTypeSymbol Retarget(FunctionPointerTypeSymbol type)
            {
                var signature = type.Signature;
                var newReturn = Retarget(signature.ReturnTypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                var newRefModifiers = RetargetModifiers(signature.RefCustomModifiers, out bool symbolModified);
                symbolModified = symbolModified || !signature.ReturnTypeWithAnnotations.IsSameAs(newReturn);

                var newParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
                ImmutableArray<ImmutableArray<CustomModifier>> newParamModifiers = default;

                var paramCount = signature.ParameterCount;
                if (paramCount > 0)
                {
                    var newParameterTypesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(paramCount);
                    var newParameterCustomModifiersBuilder = ArrayBuilder<ImmutableArray<CustomModifier>>.GetInstance(paramCount);
                    bool parametersModified = false;

                    foreach (var parameter in signature.Parameters)
                    {
                        var newParameterType = Retarget(parameter.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                        var newModifiers = RetargetModifiers(parameter.RefCustomModifiers, out bool customModifiersChanged);
                        newParameterTypesBuilder.Add(newParameterType);
                        newParameterCustomModifiersBuilder.Add(newModifiers);
                        parametersModified = parametersModified || !parameter.TypeWithAnnotations.IsSameAs(newParameterType) || customModifiersChanged;
                    }

                    if (parametersModified)
                    {
                        newParameterTypes = newParameterTypesBuilder.ToImmutableAndFree();
                        newParamModifiers = newParameterCustomModifiersBuilder.ToImmutableAndFree();
                        symbolModified = true;
                    }
                    else
                    {
                        newParameterTypesBuilder.Free();
                        newParameterCustomModifiersBuilder.Free();
                        newParameterTypes = signature.ParameterTypesWithAnnotations;
                    }
                }

                if (symbolModified)
                {
                    return type.SubstituteTypeSymbol(newReturn, newParameterTypes, newRefModifiers, newParamModifiers);
                }
                else
                {
                    return type;
                }
            }

            public static ErrorTypeSymbol Retarget(ErrorTypeSymbol type)
            {
                // TODO: if it is a missing symbol error but no longer missing in the target assembly, then we can resolve it here.

                var useSiteDiagnostic = type.GetUseSiteInfo().DiagnosticInfo;
                if (useSiteDiagnostic?.Severity == DiagnosticSeverity.Error)
                {
                    return type;
                }

                // A retargeted error symbol must trigger an error on use so that a dependent compilation won't
                // improperly succeed. We therefore ensure we have a use-site diagnostic.
                return
                    (type as ExtendedErrorTypeSymbol)?.AsUnreported() ?? // preserve diagnostic information if possible
                    new ExtendedErrorTypeSymbol(type, type.ResultKind,
                        type.ErrorInfo ?? new CSDiagnosticInfo(ErrorCode.ERR_ErrorInReferencedAssembly, type.ContainingAssembly?.Identity.GetDisplayName() ?? string.Empty), true);
            }

            public ImmutableArray<Symbol> Retarget(ImmutableArray<Symbol> arr)
            {
                return arr.SelectAsArray(
                    map: static (s, self) => self.Retarget(s),
                    arg: this);
            }

            public ImmutableArray<NamedTypeSymbol> Retarget(ImmutableArray<NamedTypeSymbol> sequence)
            {
                return sequence.SelectAsArray(
                    map: static (nts, self) =>
                    {
                        // If there is an error type in the base type list, it will end up in the interface list (rather
                        // than as the base class), so it might end up passing through here.  If it is specified using
                        // a primitive type keyword, then it will have a primitive type code, even if corlib is missing.
                        Debug.Assert(nts.TypeKind == TypeKind.Error || nts.PrimitiveTypeCode == Cci.PrimitiveTypeCode.NotPrimitive);
                        return self.Retarget(nts, RetargetOptions.RetargetPrimitiveTypesByName);
                    },
                    arg: this);
            }

            public ImmutableArray<TypeSymbol> Retarget(ImmutableArray<TypeSymbol> sequence)
            {
                return sequence.SelectAsArray(
                    map: static (ts, self) =>
                    {
                        // In incorrect code, a type parameter constraint list can contain primitive types.
                        Debug.Assert(ts.TypeKind == TypeKind.Error || ts.PrimitiveTypeCode == Cci.PrimitiveTypeCode.NotPrimitive);
                        return self.Retarget(ts, RetargetOptions.RetargetPrimitiveTypesByName);
                    },
                    arg: this);
            }

            public ImmutableArray<TypeWithAnnotations> Retarget(ImmutableArray<TypeWithAnnotations> sequence)
            {
                return sequence.SelectAsArray(
                    map: static (ts, self) => self.Retarget(ts, RetargetOptions.RetargetPrimitiveTypesByName),
                    arg: this);
            }

            public ImmutableArray<TypeParameterSymbol> Retarget(ImmutableArray<TypeParameterSymbol> list)
            {
                return list.SelectAsArray(
                    map: static (tps, self) => self.Retarget(tps),
                    arg: this);
            }

            public MethodSymbol Retarget(MethodSymbol method)
            {
                Debug.Assert(ReferenceEquals(method.ContainingModule, this.UnderlyingModule));
                Debug.Assert(ReferenceEquals(method, method.OriginalDefinition));

                return (MethodSymbol)this.SymbolMap.GetOrAdd(method, _retargetingModule._createRetargetingMethod);
            }

            public MethodSymbol Retarget(MethodSymbol method, IEqualityComparer<MethodSymbol> retargetedMethodComparer)
            {
                Debug.Assert((object)method == method.ConstructedFrom);

                if (ReferenceEquals(method.ContainingModule, this.UnderlyingModule) && ReferenceEquals(method, method.OriginalDefinition))
                {
                    return Retarget(method);
                }

                var containingType = method.ContainingType;
                var retargetedType = Retarget(containingType, RetargetOptions.RetargetPrimitiveTypesByName);

                if (ReferenceEquals(retargetedType, containingType))
                {
                    return method;
                }

                if (!containingType.IsDefinition)
                {
                    Debug.Assert(!retargetedType.IsDefinition);

                    var retargetedDefinition = Retarget(method.OriginalDefinition, retargetedMethodComparer);

                    if (retargetedDefinition is null)
                    {
                        return null;
                    }

                    return retargetedDefinition.AsMember(retargetedType);
                }

                Debug.Assert(retargetedType.IsDefinition);

                // NB: may return null if the method cannot be found in the retargeted type (e.g. removed in a subsequent version)
                return FindMethodInRetargetedType(method, retargetedType, retargetedMethodComparer);
            }

            public FieldSymbol Retarget(FieldSymbol field)
            {
                return (FieldSymbol)this.SymbolMap.GetOrAdd(field, _retargetingModule._createRetargetingField);
            }

            public PropertySymbol Retarget(PropertySymbol property)
            {
                Debug.Assert(ReferenceEquals(property.ContainingModule, this.UnderlyingModule));
                Debug.Assert(ReferenceEquals(property, property.OriginalDefinition));

                return (PropertySymbol)this.SymbolMap.GetOrAdd(property, _retargetingModule._createRetargetingProperty);
            }

            public PropertySymbol Retarget(PropertySymbol property, IEqualityComparer<PropertySymbol> retargetedPropertyComparer)
            {
                if (ReferenceEquals(property.ContainingModule, this.UnderlyingModule) && ReferenceEquals(property, property.OriginalDefinition))
                {
                    return Retarget(property);
                }

                var containingType = property.ContainingType;
                var retargetedType = Retarget(containingType, RetargetOptions.RetargetPrimitiveTypesByName);

                // NB: may return null if the property cannot be found in the retargeted type (e.g. removed in a subsequent version)
                return ReferenceEquals(retargetedType, containingType) ?
                           property :
                           FindPropertyInRetargetedType(property, retargetedType, retargetedPropertyComparer);
            }

            public EventSymbol Retarget(EventSymbol @event)
            {
                if (ReferenceEquals(@event.ContainingModule, this.UnderlyingModule) && ReferenceEquals(@event, @event.OriginalDefinition))
                {
                    return (EventSymbol)this.SymbolMap.GetOrAdd(@event, _retargetingModule._createRetargetingEvent);
                }

                var containingType = @event.ContainingType;
                var retargetedType = Retarget(containingType, RetargetOptions.RetargetPrimitiveTypesByName);

                // NB: may return null if the event cannot be found in the retargeted type (e.g. removed in a subsequent version)
                return ReferenceEquals(retargetedType, containingType) ?
                           @event :
                           FindEventInRetargetedType(@event, retargetedType);
            }

            private MethodSymbol FindMethodInRetargetedType(MethodSymbol method, NamedTypeSymbol retargetedType, IEqualityComparer<MethodSymbol> retargetedMethodComparer)
            {
                return RetargetedTypeMethodFinder.Find(this, method, retargetedType, retargetedMethodComparer);
            }

            private class RetargetedTypeMethodFinder : RetargetingSymbolTranslator
            {
                private readonly NamedTypeSymbol _retargetedType;
                private readonly MethodSymbol _toFind;

                private RetargetedTypeMethodFinder(RetargetingModuleSymbol retargetingModule, NamedTypeSymbol retargetedType, MethodSymbol toFind) :
                    base(retargetingModule)
                {
                    _retargetedType = retargetedType;
                    _toFind = toFind;
                }

                public static MethodSymbol Find(RetargetingSymbolTranslator translator, MethodSymbol method, NamedTypeSymbol retargetedType, IEqualityComparer<MethodSymbol> retargetedMethodComparer)
                {
                    if (!method.IsGenericMethod && !retargetedType.IsGenericType)
                    {
                        return FindWorker(translator, method, retargetedType, retargetedMethodComparer);
                    }

                    // A generic method or a method in generic type needs special handling because its signature is very likely
                    // to refer to method's or type's type parameters.
                    var finder = new RetargetedTypeMethodFinder(translator._retargetingModule, retargetedType, method);
                    return FindWorker(finder, method, retargetedType, retargetedMethodComparer);
                }

                private static MethodSymbol FindWorker
                (
                    RetargetingSymbolTranslator translator,
                    MethodSymbol method,
                    NamedTypeSymbol retargetedType,
                    IEqualityComparer<MethodSymbol> retargetedMethodComparer
                )
                {
                    var targetParams = method.Parameters.SelectAsArray(
                        map: static ParameterSymbol (param, translator) => new SignatureOnlyParameterSymbol(
                            translator.Retarget(param.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                            translator.RetargetModifiers(param.RefCustomModifiers, modifiersHaveChanged: out _),
                            isParamsArray: param.IsParamsArray,
                            isParamsCollection: param.IsParamsCollection,
                            param.RefKind),
                        arg: translator);

                    // We will be using this symbol only for the purpose of method signature comparison,
                    // IndexedTypeParameterSymbols should work just fine as the type parameters for the method.
                    // We can't produce "real" TypeParameterSymbols without finding the method first and this
                    // is what we are trying to do right now.
                    var targetMethod = new SignatureOnlyMethodSymbol(
                        method.Name,
                        retargetedType,
                        method.MethodKind,
                        method.CallingConvention,
                        IndexedTypeParameterSymbol.TakeSymbols(method.Arity),
                        targetParams,
                        method.RefKind,
                        method.IsInitOnly,
                        method.IsStatic,
                        translator.Retarget(method.ReturnTypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                        translator.RetargetModifiers(method.RefCustomModifiers, modifiersHaveChanged: out _),
                        ImmutableArray<MethodSymbol>.Empty);

                    foreach (var retargetedMember in retargetedType.GetMembers(method.Name))
                    {
                        if (retargetedMember.Kind == SymbolKind.Method)
                        {
                            var retargetedMethod = (MethodSymbol)retargetedMember;
                            if (retargetedMethodComparer.Equals(retargetedMethod, targetMethod))
                            {
                                return retargetedMethod;
                            }
                        }
                    }

                    return null;
                }

                public override TypeParameterSymbol Retarget(TypeParameterSymbol typeParameter)
                {
                    if (typeParameter.TypeParameterKind == TypeParameterKind.Method)
                    {
                        Debug.Assert((object)typeParameter.ContainingSymbol == _toFind);

                        // The method symbol we are building will be using IndexedTypeParameterSymbols as 
                        // its type parameters, therefore, we should return them here as well.
                        return IndexedTypeParameterSymbol.GetTypeParameter(typeParameter.Ordinal);
                    }

                    NamedTypeSymbol containingType = _toFind.ContainingType;
                    NamedTypeSymbol retargetedContainingType = _retargetedType;

                    do
                    {
                        if ((object)containingType == typeParameter.ContainingSymbol)
                        {
                            return retargetedContainingType.TypeParameters[typeParameter.Ordinal];
                        }

                        containingType = containingType.ContainingType;
                        retargetedContainingType = retargetedContainingType.ContainingType;
                    }
                    while (containingType is object);

                    throw ExceptionUtilities.Unreachable();
                }
            }

            private PropertySymbol FindPropertyInRetargetedType(PropertySymbol property, NamedTypeSymbol retargetedType, IEqualityComparer<PropertySymbol> retargetedPropertyComparer)
            {
                var targetParams = property.Parameters.SelectAsArray(
                    map: static ParameterSymbol (param, self) => new SignatureOnlyParameterSymbol(
                        self.Retarget(param.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                        self.RetargetModifiers(param.RefCustomModifiers, modifiersHaveChanged: out _),
                        isParamsArray: param.IsParamsArray,
                        isParamsCollection: param.IsParamsCollection,
                        param.RefKind),
                    arg: this);

                var targetProperty = new SignatureOnlyPropertySymbol(
                    property.Name,
                    retargetedType,
                    targetParams,
                    property.RefKind,
                    Retarget(property.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode),
                    RetargetModifiers(property.RefCustomModifiers, modifiersHaveChanged: out _),
                    property.IsStatic,
                    ImmutableArray<PropertySymbol>.Empty);

                foreach (var retargetedMember in retargetedType.GetMembers(property.Name))
                {
                    if (retargetedMember.Kind == SymbolKind.Property)
                    {
                        var retargetedProperty = (PropertySymbol)retargetedMember;
                        if (retargetedPropertyComparer.Equals(retargetedProperty, targetProperty))
                        {
                            return retargetedProperty;
                        }
                    }
                }

                return null;
            }

            private EventSymbol FindEventInRetargetedType(EventSymbol @event, NamedTypeSymbol retargetedType)
            {
                var targetType = Retarget(@event.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode);

                foreach (var retargetedMember in retargetedType.GetMembers(@event.Name))
                {
                    if (retargetedMember.Kind == SymbolKind.Event)
                    {
                        var retargetedEvent = (EventSymbol)retargetedMember;
                        if (TypeSymbol.Equals(retargetedEvent.Type, targetType.Type, TypeCompareKind.ConsiderEverything2))
                        {
                            return retargetedEvent;
                        }
                    }
                }

                return null;
            }

            internal ImmutableArray<CustomModifier> RetargetModifiers(
                ImmutableArray<CustomModifier> oldModifiers,
                ref ImmutableArray<CustomModifier> lazyCustomModifiers)
            {
                if (lazyCustomModifiers.IsDefault)
                {
                    bool modifiersHaveChanged;
                    ImmutableArray<CustomModifier> newModifiers = this.RetargetModifiers(oldModifiers, out modifiersHaveChanged);
                    ImmutableInterlocked.InterlockedCompareExchange(ref lazyCustomModifiers, newModifiers, default(ImmutableArray<CustomModifier>));
                }

                return lazyCustomModifiers;
            }

            private ImmutableArray<CSharpAttributeData> RetargetAttributes(ImmutableArray<CSharpAttributeData> oldAttributes)
            {
                return oldAttributes.SelectAsArray(map: (a, t) => t.RetargetAttributeData(a), arg: this);
            }

            internal IEnumerable<CSharpAttributeData> RetargetAttributes(IEnumerable<CSharpAttributeData> attributes)
            {
                foreach (var attributeData in attributes)
                {
                    yield return this.RetargetAttributeData(attributeData);
                }
            }

            private CSharpAttributeData RetargetAttributeData(CSharpAttributeData oldAttributeData)
            {
                MethodSymbol oldAttributeCtor = oldAttributeData.AttributeConstructor;
                MethodSymbol newAttributeCtor = (object)oldAttributeCtor == null ?
                    null :
                    Retarget(oldAttributeCtor, MemberSignatureComparer.RetargetedExplicitImplementationComparer);

                NamedTypeSymbol oldAttributeType = oldAttributeData.AttributeClass;
                NamedTypeSymbol newAttributeType;
                if ((object)newAttributeCtor != null)
                {
                    newAttributeType = newAttributeCtor.ContainingType;
                }
                else if ((object)oldAttributeType != null)
                {
                    newAttributeType = Retarget(oldAttributeType, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                }
                else
                {
                    newAttributeType = null;
                }

                ImmutableArray<TypedConstant> oldAttributeCtorArguments = oldAttributeData.CommonConstructorArguments;
                ImmutableArray<TypedConstant> newAttributeCtorArguments = RetargetAttributeConstructorArguments(oldAttributeCtorArguments);

                ImmutableArray<KeyValuePair<string, TypedConstant>> oldAttributeNamedArguments = oldAttributeData.CommonNamedArguments;
                ImmutableArray<KeyValuePair<string, TypedConstant>> newAttributeNamedArguments = RetargetAttributeNamedArguments(oldAttributeNamedArguments);

                // Must create a RetargetingAttributeData even if the types and
                // arguments are unchanged since the AttributeData instance is
                // used to resolve System.Type which may require retargeting.
                return new RetargetingAttributeData(
                    oldAttributeData,
                    newAttributeType,
                    newAttributeCtor,
                    newAttributeCtorArguments,
                    newAttributeNamedArguments);
            }

            private ImmutableArray<TypedConstant> RetargetAttributeConstructorArguments(ImmutableArray<TypedConstant> constructorArguments)
            {
                ImmutableArray<TypedConstant> retargetedArguments = constructorArguments;
                bool argumentsHaveChanged = false;

                if (!constructorArguments.IsDefault && constructorArguments.Any())
                {
                    var newArguments = ArrayBuilder<TypedConstant>.GetInstance(constructorArguments.Length);

                    foreach (TypedConstant oldArgument in constructorArguments)
                    {
                        TypedConstant retargetedArgument = RetargetTypedConstant(oldArgument, ref argumentsHaveChanged);
                        newArguments.Add(retargetedArgument);
                    }

                    if (argumentsHaveChanged)
                    {
                        retargetedArguments = newArguments.ToImmutable();
                    }

                    newArguments.Free();
                }

                return retargetedArguments;
            }

            private TypedConstant RetargetTypedConstant(TypedConstant oldConstant, ref bool typedConstantChanged)
            {
                TypeSymbol oldConstantType = (TypeSymbol)oldConstant.TypeInternal;
                TypeSymbol newConstantType = (object)oldConstantType == null ?
                    null :
                    Retarget(oldConstantType, RetargetOptions.RetargetPrimitiveTypesByTypeCode);

                if (oldConstant.Kind == TypedConstantKind.Array)
                {
                    var newArray = RetargetAttributeConstructorArguments(oldConstant.Values);
                    if (!TypeSymbol.Equals(newConstantType, oldConstantType, TypeCompareKind.ConsiderEverything2) || newArray != oldConstant.Values)
                    {
                        typedConstantChanged = true;
                        return new TypedConstant(newConstantType, newArray);
                    }
                    else
                    {
                        return oldConstant;
                    }
                }

                object newConstantValue;
                object oldConstantValue = oldConstant.ValueInternal;
                if ((oldConstant.Kind == TypedConstantKind.Type) && (oldConstantValue != null))
                {
                    newConstantValue = Retarget((TypeSymbol)oldConstantValue, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                }
                else
                {
                    newConstantValue = oldConstantValue;
                }

                if (!TypeSymbol.Equals(newConstantType, oldConstantType, TypeCompareKind.ConsiderEverything2) || newConstantValue != oldConstantValue)
                {
                    typedConstantChanged = true;
                    return new TypedConstant(newConstantType, oldConstant.Kind, newConstantValue);
                }
                else
                {
                    return oldConstant;
                }
            }

            private ImmutableArray<KeyValuePair<string, TypedConstant>> RetargetAttributeNamedArguments(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
            {
                var retargetedArguments = namedArguments;
                bool argumentsHaveChanged = false;

                if (namedArguments.Any())
                {
                    var newArguments = ArrayBuilder<KeyValuePair<string, TypedConstant>>.GetInstance(namedArguments.Length);

                    foreach (KeyValuePair<string, TypedConstant> oldArgument in namedArguments)
                    {
                        TypedConstant oldConstant = oldArgument.Value;
                        bool typedConstantChanged = false;
                        TypedConstant newConstant = RetargetTypedConstant(oldConstant, ref typedConstantChanged);

                        if (typedConstantChanged)
                        {
                            newArguments.Add(new KeyValuePair<string, TypedConstant>(oldArgument.Key, newConstant));
                            argumentsHaveChanged = true;
                        }
                        else
                        {
                            newArguments.Add(oldArgument);
                        }
                    }

                    if (argumentsHaveChanged)
                    {
                        retargetedArguments = newArguments.ToImmutable();
                    }

                    newArguments.Free();
                }

                return retargetedArguments;
            }

            // Get the retargeted attributes
            internal ImmutableArray<CSharpAttributeData> GetRetargetedAttributes(
                ImmutableArray<CSharpAttributeData> underlyingAttributes,
                ref ImmutableArray<CSharpAttributeData> lazyCustomAttributes)
            {
                if (lazyCustomAttributes.IsDefault)
                {
                    // Retarget the attributes
                    ImmutableArray<CSharpAttributeData> retargetedAttributes = this.RetargetAttributes(underlyingAttributes);

                    ImmutableInterlocked.InterlockedCompareExchange(ref lazyCustomAttributes, retargetedAttributes, default(ImmutableArray<CSharpAttributeData>));
                }

                return lazyCustomAttributes;
            }

            public override Symbol VisitModule(ModuleSymbol symbol, RetargetOptions options)
            {
                // We shouldn't run into any other module, but the underlying module
                Debug.Assert(ReferenceEquals(symbol, _retargetingModule.UnderlyingModule));
                return _retargetingModule;
            }

            public override Symbol VisitNamespace(NamespaceSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitNamedType(NamedTypeSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol, options);
            }

            public override Symbol VisitArrayType(ArrayTypeSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitPointerType(PointerTypeSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitFunctionPointerType(FunctionPointerTypeSymbol symbol, RetargetOptions argument)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitMethod(MethodSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitParameter(ParameterSymbol symbol, RetargetOptions options)
            {
                throw ExceptionUtilities.Unreachable();
            }

            public override Symbol VisitField(FieldSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitProperty(PropertySymbol symbol, RetargetOptions argument)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitTypeParameter(TypeParameterSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitErrorType(ErrorTypeSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitEvent(EventSymbol symbol, RetargetOptions options)
            {
                return Retarget(symbol);
            }

            public override Symbol VisitDynamicType(DynamicTypeSymbol symbol, RetargetOptions argument)
            {
                // TODO(cyrusn): What's the right thing to do here?
                return symbol;
            }
        }
    }
}
