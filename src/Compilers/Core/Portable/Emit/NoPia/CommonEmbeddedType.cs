// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.NoPia
{
    internal abstract partial class EmbeddedTypesManager<
        TPEModuleBuilder,
        TModuleCompilationState,
        TEmbeddedTypesManager,
        TSyntaxNode,
        TAttributeData,
        TSymbol,
        TAssemblySymbol,
        TNamedTypeSymbol,
        TFieldSymbol,
        TMethodSymbol,
        TEventSymbol,
        TPropertySymbol,
        TParameterSymbol,
        TTypeParameterSymbol,
        TEmbeddedType,
        TEmbeddedField,
        TEmbeddedMethod,
        TEmbeddedEvent,
        TEmbeddedProperty,
        TEmbeddedParameter,
        TEmbeddedTypeParameter>
    {
        internal abstract class CommonEmbeddedType : Cci.INamespaceTypeDefinition
        {
            public readonly TEmbeddedTypesManager TypeManager;
            public readonly TNamedTypeSymbol UnderlyingNamedType;

            private ImmutableArray<Cci.IFieldDefinition> _lazyFields;
            private ImmutableArray<Cci.IMethodDefinition> _lazyMethods;
            private ImmutableArray<Cci.IPropertyDefinition> _lazyProperties;
            private ImmutableArray<Cci.IEventDefinition> _lazyEvents;
            private ImmutableArray<TAttributeData> _lazyAttributes;
            private int _lazyAssemblyRefIndex = -1;

            protected CommonEmbeddedType(TEmbeddedTypesManager typeManager, TNamedTypeSymbol underlyingNamedType)
            {
                this.TypeManager = typeManager;
                this.UnderlyingNamedType = underlyingNamedType;
            }

            protected abstract int GetAssemblyRefIndex();

            protected abstract IEnumerable<TFieldSymbol> GetFieldsToEmit();
            protected abstract IEnumerable<TMethodSymbol> GetMethodsToEmit();
            protected abstract IEnumerable<TEventSymbol> GetEventsToEmit();
            protected abstract IEnumerable<TPropertySymbol> GetPropertiesToEmit();
            protected abstract bool IsPublic { get; }
            protected abstract bool IsAbstract { get; }
            protected abstract Cci.ITypeReference GetBaseClass(TPEModuleBuilder moduleBuilder, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
            protected abstract IEnumerable<Cci.TypeReferenceWithAttributes> GetInterfaces(EmitContext context);
            protected abstract bool IsBeforeFieldInit { get; }
            protected abstract bool IsComImport { get; }
            protected abstract bool IsInterface { get; }
            protected abstract bool IsDelegate { get; }
            protected abstract bool IsSerializable { get; }
            protected abstract bool IsSpecialName { get; }
            protected abstract bool IsWindowsRuntimeImport { get; }
            protected abstract bool IsSealed { get; }
            protected abstract TypeLayout? GetTypeLayoutIfStruct();
            protected abstract System.Runtime.InteropServices.CharSet StringFormat { get; }
            protected abstract TAttributeData CreateTypeIdentifierAttribute(bool hasGuid, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
            protected abstract void EmbedDefaultMembers(string defaultMember, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
            protected abstract IEnumerable<TAttributeData> GetCustomAttributesToEmit(TPEModuleBuilder moduleBuilder);
            protected abstract void ReportMissingAttribute(AttributeDescription description, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);

            private bool IsTargetAttribute(TAttributeData attrData, AttributeDescription description)
            {
                return TypeManager.IsTargetAttribute(UnderlyingNamedType, attrData, description);
            }

            private ImmutableArray<TAttributeData> GetAttributes(TPEModuleBuilder moduleBuilder, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                var builder = ArrayBuilder<TAttributeData>.GetInstance();

                // Put the CompilerGenerated attribute on the NoPIA types we define so that 
                // static analysis tools (e.g. fxcop) know that they can be skipped
                builder.AddOptional(TypeManager.CreateCompilerGeneratedAttribute());

                // Copy some of the attributes.

                bool hasGuid = false;
                bool hasComEventInterfaceAttribute = false;

                // Note, when porting attributes, we are not using constructors from original symbol.
                // The constructors might be missing (for example, in metadata case) and doing lookup
                // will ensure that we report appropriate errors.

                foreach (var attrData in GetCustomAttributesToEmit(moduleBuilder))
                {
                    if (IsTargetAttribute(attrData, AttributeDescription.GuidAttribute))
                    {
                        string guidString;
                        if (attrData.TryGetGuidAttributeValue(out guidString))
                        {
                            // If this type has a GuidAttribute, we should emit it.
                            hasGuid = true;
                            builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_GuidAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                        }
                    }
                    else if (IsTargetAttribute(attrData, AttributeDescription.ComEventInterfaceAttribute))
                    {
                        if (attrData.CommonConstructorArguments.Length == 2)
                        {
                            hasComEventInterfaceAttribute = true;
                            builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_ComEventInterfaceAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                        }
                    }
                    else
                    {
                        int signatureIndex = TypeManager.GetTargetAttributeSignatureIndex(UnderlyingNamedType, attrData, AttributeDescription.InterfaceTypeAttribute);
                        if (signatureIndex != -1)
                        {
                            Debug.Assert(signatureIndex == 0 || signatureIndex == 1);
                            if (attrData.CommonConstructorArguments.Length == 1)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(signatureIndex == 0 ? WellKnownMember.System_Runtime_InteropServices_InterfaceTypeAttribute__ctorInt16 :
                                    WellKnownMember.System_Runtime_InteropServices_InterfaceTypeAttribute__ctorComInterfaceType,
                                    attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                        else if (IsTargetAttribute(attrData, AttributeDescription.BestFitMappingAttribute))
                        {
                            if (attrData.CommonConstructorArguments.Length == 1)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_BestFitMappingAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                        else if (IsTargetAttribute(attrData, AttributeDescription.CoClassAttribute))
                        {
                            if (attrData.CommonConstructorArguments.Length == 1)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_CoClassAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                        else if (IsTargetAttribute(attrData, AttributeDescription.FlagsAttribute))
                        {
                            if (attrData.CommonConstructorArguments.Length == 0 && UnderlyingNamedType.IsEnum)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_FlagsAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                        else if (IsTargetAttribute(attrData, AttributeDescription.DefaultMemberAttribute))
                        {
                            if (attrData.CommonConstructorArguments.Length == 1)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));

                                // Embed members matching default member name.
                                string defaultMember = attrData.CommonConstructorArguments[0].ValueInternal as string;
                                if (defaultMember != null)
                                {
                                    EmbedDefaultMembers(defaultMember, syntaxNodeOpt, diagnostics);
                                }
                            }
                        }
                        else if (IsTargetAttribute(attrData, AttributeDescription.UnmanagedFunctionPointerAttribute))
                        {
                            if (attrData.CommonConstructorArguments.Length == 1)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                    }
                }

                // We must emit a TypeIdentifier attribute which connects this local type with the canonical type. 
                // Interfaces usually have a guid attribute, in which case the TypeIdentifier attribute we emit will
                // not need any additional parameters. For interfaces which lack a guid and all other types, we must 
                // emit a TypeIdentifier that has parameters identifying the scope and name of the original type. We 
                // will use the Assembly GUID as the scope identifier.

                if (IsInterface && !hasComEventInterfaceAttribute)
                {
                    if (!IsComImport)
                    {
                        // If we have an interface not marked ComImport, but the assembly is linked, then
                        // we need to give an error. We allow event interfaces to not have ComImport marked on them.
                        // ERRID_NoPIAAttributeMissing2/ERR_InteropTypeMissingAttribute
                        ReportMissingAttribute(AttributeDescription.ComImportAttribute, syntaxNodeOpt, diagnostics);
                    }
                    else if (!hasGuid)
                    {
                        // Interfaces used with No-PIA ought to have a guid attribute, or the CLR cannot do type unification. 
                        // This interface lacks a guid, so unification probably won't work. We allow event interfaces to not have a Guid.
                        // ERRID_NoPIAAttributeMissing2/ERR_InteropTypeMissingAttribute
                        ReportMissingAttribute(AttributeDescription.GuidAttribute, syntaxNodeOpt, diagnostics);
                    }
                }

                // Note, this logic should match the one in RetargetingSymbolTranslator.RetargetNoPiaLocalType
                // when we try to predict what attributes we will emit on embedded type, which corresponds the 
                // type we are retargeting.

                builder.AddOptional(CreateTypeIdentifierAttribute(hasGuid && IsInterface, syntaxNodeOpt, diagnostics));

                return builder.ToImmutableAndFree();
            }

            public int AssemblyRefIndex
            {
                get
                {
                    if (_lazyAssemblyRefIndex == -1)
                    {
                        _lazyAssemblyRefIndex = GetAssemblyRefIndex();
                        Debug.Assert(_lazyAssemblyRefIndex >= 0);
                    }
                    return _lazyAssemblyRefIndex;
                }
            }

            bool Cci.INamespaceTypeDefinition.IsPublic
            {
                get
                {
                    return IsPublic;
                }
            }

            Cci.ITypeReference Cci.ITypeDefinition.GetBaseClass(EmitContext context)
            {
                return GetBaseClass((TPEModuleBuilder)context.Module, (TSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
            }

            IEnumerable<Cci.IEventDefinition> Cci.ITypeDefinition.GetEvents(EmitContext context)
            {
                if (_lazyEvents.IsDefault)
                {
                    Debug.Assert(TypeManager.IsFrozen);

                    var builder = ArrayBuilder<Cci.IEventDefinition>.GetInstance();

                    foreach (var e in GetEventsToEmit())
                    {
                        TEmbeddedEvent embedded;

                        if (TypeManager.EmbeddedEventsMap.TryGetValue(e, out embedded))
                        {
                            builder.Add(embedded);
                        }
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyEvents, builder.ToImmutableAndFree());
                }

                return _lazyEvents;
            }

            IEnumerable<Cci.MethodImplementation> Cci.ITypeDefinition.GetExplicitImplementationOverrides(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<Cci.MethodImplementation>();
            }

            IEnumerable<Cci.IFieldDefinition> Cci.ITypeDefinition.GetFields(EmitContext context)
            {
                if (_lazyFields.IsDefault)
                {
                    Debug.Assert(TypeManager.IsFrozen);

                    var builder = ArrayBuilder<Cci.IFieldDefinition>.GetInstance();

                    foreach (var f in GetFieldsToEmit())
                    {
                        TEmbeddedField embedded;

                        if (TypeManager.EmbeddedFieldsMap.TryGetValue(f, out embedded))
                        {
                            builder.Add(embedded);
                        }
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyFields, builder.ToImmutableAndFree());
                }

                return _lazyFields;
            }

            IEnumerable<Cci.IGenericTypeParameter> Cci.ITypeDefinition.GenericParameters
            {
                get
                {
                    return SpecializedCollections.EmptyEnumerable<Cci.IGenericTypeParameter>();
                }
            }

            ushort Cci.ITypeDefinition.GenericParameterCount
            {
                get
                {
                    return 0;
                }
            }

            bool Cci.ITypeDefinition.HasDeclarativeSecurity
            {
                get
                {
                    // None of the transferrable attributes are security attributes.
                    return false;
                }
            }

            IEnumerable<Cci.TypeReferenceWithAttributes> Cci.ITypeDefinition.Interfaces(EmitContext context)
            {
                return GetInterfaces(context);
            }

            bool Cci.ITypeDefinition.IsAbstract
            {
                get
                {
                    return IsAbstract;
                }
            }

            bool Cci.ITypeDefinition.IsBeforeFieldInit
            {
                get
                {
                    return IsBeforeFieldInit;
                }
            }

            bool Cci.ITypeDefinition.IsComObject
            {
                get
                {
                    return IsInterface || IsComImport;
                }
            }

            bool Cci.ITypeDefinition.IsGeneric
            {
                get
                {
                    return false;
                }
            }

            bool Cci.ITypeDefinition.IsInterface
            {
                get
                {
                    return IsInterface;
                }
            }

            bool Cci.ITypeDefinition.IsDelegate
            {
                get
                {
                    return IsDelegate;
                }
            }

            bool Cci.ITypeDefinition.IsRuntimeSpecial
            {
                get
                {
                    return false;
                }
            }

            bool Cci.ITypeDefinition.IsSerializable
            {
                get
                {
                    return IsSerializable;
                }
            }

            bool Cci.ITypeDefinition.IsSpecialName
            {
                get
                {
                    return IsSpecialName;
                }
            }

            bool Cci.ITypeDefinition.IsWindowsRuntimeImport
            {
                get
                {
                    return IsWindowsRuntimeImport;
                }
            }

            bool Cci.ITypeDefinition.IsSealed
            {
                get
                {
                    return IsSealed;
                }
            }

            System.Runtime.InteropServices.LayoutKind Cci.ITypeDefinition.Layout
            {
                get
                {
                    var layout = GetTypeLayoutIfStruct();
                    return layout?.Kind ?? System.Runtime.InteropServices.LayoutKind.Auto;
                }
            }

            ushort Cci.ITypeDefinition.Alignment
            {
                get
                {
                    var layout = GetTypeLayoutIfStruct();
                    return (ushort)(layout?.Alignment ?? 0);
                }
            }

            uint Cci.ITypeDefinition.SizeOf
            {
                get
                {
                    var layout = GetTypeLayoutIfStruct();
                    return (uint)(layout?.Size ?? 0);
                }
            }

            IEnumerable<Cci.IMethodDefinition> Cci.ITypeDefinition.GetMethods(EmitContext context)
            {
                if (_lazyMethods.IsDefault)
                {
                    Debug.Assert(TypeManager.IsFrozen);

                    var builder = ArrayBuilder<Cci.IMethodDefinition>.GetInstance();

                    int gapIndex = 1;
                    int gapSize = 0;

                    foreach (var method in GetMethodsToEmit())
                    {
                        if ((object)method != null)
                        {
                            TEmbeddedMethod embedded;

                            if (TypeManager.EmbeddedMethodsMap.TryGetValue(method, out embedded))
                            {
                                if (gapSize > 0)
                                {
                                    builder.Add(new VtblGap(this, ModuleExtensions.GetVTableGapName(gapIndex, gapSize)));
                                    gapIndex++;
                                    gapSize = 0;
                                }

                                builder.Add(embedded);
                            }
                            else
                            {
                                gapSize++;
                            }
                        }
                        else
                        {
                            gapSize++;
                        }
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyMethods, builder.ToImmutableAndFree());
                }

                return _lazyMethods;
            }

            IEnumerable<Cci.INestedTypeDefinition> Cci.ITypeDefinition.GetNestedTypes(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<Cci.INestedTypeDefinition>();
            }

            IEnumerable<Cci.IPropertyDefinition> Cci.ITypeDefinition.GetProperties(EmitContext context)
            {
                if (_lazyProperties.IsDefault)
                {
                    Debug.Assert(TypeManager.IsFrozen);

                    var builder = ArrayBuilder<Cci.IPropertyDefinition>.GetInstance();

                    foreach (var p in GetPropertiesToEmit())
                    {
                        TEmbeddedProperty embedded;

                        if (TypeManager.EmbeddedPropertiesMap.TryGetValue(p, out embedded))
                        {
                            builder.Add(embedded);
                        }
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyProperties, builder.ToImmutableAndFree());
                }

                return _lazyProperties;
            }

            IEnumerable<Cci.SecurityAttribute> Cci.ITypeDefinition.SecurityAttributes
            {
                get
                {
                    // None of the transferrable attributes are security attributes.
                    return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
                }
            }

            System.Runtime.InteropServices.CharSet Cci.ITypeDefinition.StringFormat
            {
                get
                {
                    return StringFormat;
                }
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
            {
                if (_lazyAttributes.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var attributes = GetAttributes((TPEModuleBuilder)context.Module, (TSyntaxNode)context.SyntaxNodeOpt, diagnostics);

                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyAttributes, attributes))
                    {
                        // Save any diagnostics that we encountered.
                        context.Diagnostics.AddRange(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyAttributes;
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                throw ExceptionUtilities.Unreachable;
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            bool Cci.ITypeReference.IsEnum
            {
                get
                {
                    return UnderlyingNamedType.IsEnum;
                }
            }

            bool Cci.ITypeReference.IsValueType
            {
                get
                {
                    return UnderlyingNamedType.IsValueType;
                }
            }

            Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
            {
                return this;
            }

            Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
            {
                get
                {
                    return Cci.PrimitiveTypeCode.NotPrimitive;
                }
            }

            TypeDefinitionHandle Cci.ITypeReference.TypeDef
            {
                get
                {
                    return default(TypeDefinitionHandle);
                }
            }

            Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
            {
                get
                {
                    return null;
                }
            }

            Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
            {
                get
                {
                    return null;
                }
            }

            Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
            {
                get
                {
                    return null;
                }
            }

            Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
            {
                return this;
            }

            Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
            {
                get
                {
                    return this;
                }
            }

            Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
            {
                return null;
            }

            Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
            {
                get
                {
                    return null;
                }
            }

            Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
            {
                get
                {
                    return null;
                }
            }

            Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
            {
                return this;
            }

            ushort Cci.INamedTypeReference.GenericParameterCount
            {
                get
                {
                    return 0;
                }
            }

            bool Cci.INamedTypeReference.MangleName
            {
                get
                {
                    return UnderlyingNamedType.MangleName;
                }
            }

            string Cci.INamedEntity.Name
            {
                get
                {
                    return UnderlyingNamedType.Name;
                }
            }

            Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
            {
                return TypeManager.ModuleBeingBuilt;
            }

            string Cci.INamespaceTypeReference.NamespaceName
            {
                get
                {
                    return UnderlyingNamedType.NamespaceName;
                }
            }

            /// <remarks>
            /// This is only used for testing.
            /// </remarks>
            public override string ToString()
            {
                return ((ISymbolInternal)UnderlyingNamedType).GetISymbol().ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
            }
        }
    }
}
