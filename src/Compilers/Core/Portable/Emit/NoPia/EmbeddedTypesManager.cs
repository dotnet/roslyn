// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.NoPia
{
    internal abstract class CommonEmbeddedTypesManager
    {
        public abstract bool IsFrozen { get; }
        public abstract ImmutableArray<Cci.INamespaceTypeDefinition> GetTypes(DiagnosticBag diagnostics, HashSet<string> namesOfTopLevelTypes);
    }

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
        TEmbeddedTypeParameter> : CommonEmbeddedTypesManager
        where TPEModuleBuilder : CommonPEModuleBuilder
        where TModuleCompilationState : CommonModuleCompilationState
        where TEmbeddedTypesManager : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>
        where TSyntaxNode : SyntaxNode
        where TAttributeData : class, Cci.ICustomAttribute
        where TAssemblySymbol : class
        where TNamedTypeSymbol : class, TSymbol, Cci.INamespaceTypeReference
        where TFieldSymbol : class, TSymbol, Cci.IFieldReference
        where TMethodSymbol : class, TSymbol, Cci.IMethodReference
        where TEventSymbol : class, TSymbol, Cci.ITypeMemberReference
        where TPropertySymbol : class, TSymbol, Cci.ITypeMemberReference
        where TParameterSymbol : class, TSymbol, Cci.IParameterListEntry, Cci.INamedEntity
        where TTypeParameterSymbol : class, TSymbol, Cci.IGenericMethodParameterReference
        where TEmbeddedType : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedType
        where TEmbeddedField : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedField
        where TEmbeddedMethod : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedMethod
        where TEmbeddedEvent : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedEvent
        where TEmbeddedProperty : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedProperty
        where TEmbeddedParameter : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedParameter
        where TEmbeddedTypeParameter : EmbeddedTypesManager<TPEModuleBuilder, TModuleCompilationState, TEmbeddedTypesManager, TSyntaxNode, TAttributeData, TSymbol, TAssemblySymbol, TNamedTypeSymbol, TFieldSymbol, TMethodSymbol, TEventSymbol, TPropertySymbol, TParameterSymbol, TTypeParameterSymbol, TEmbeddedType, TEmbeddedField, TEmbeddedMethod, TEmbeddedEvent, TEmbeddedProperty, TEmbeddedParameter, TEmbeddedTypeParameter>.CommonEmbeddedTypeParameter
    {
        public readonly TPEModuleBuilder ModuleBeingBuilt;

        public readonly ConcurrentDictionary<TNamedTypeSymbol, TEmbeddedType> EmbeddedTypesMap = new ConcurrentDictionary<TNamedTypeSymbol, TEmbeddedType>(ReferenceEqualityComparer.Instance);
        public readonly ConcurrentDictionary<TFieldSymbol, TEmbeddedField> EmbeddedFieldsMap = new ConcurrentDictionary<TFieldSymbol, TEmbeddedField>(ReferenceEqualityComparer.Instance);
        public readonly ConcurrentDictionary<TMethodSymbol, TEmbeddedMethod> EmbeddedMethodsMap = new ConcurrentDictionary<TMethodSymbol, TEmbeddedMethod>(ReferenceEqualityComparer.Instance);
        public readonly ConcurrentDictionary<TPropertySymbol, TEmbeddedProperty> EmbeddedPropertiesMap = new ConcurrentDictionary<TPropertySymbol, TEmbeddedProperty>(ReferenceEqualityComparer.Instance);
        public readonly ConcurrentDictionary<TEventSymbol, TEmbeddedEvent> EmbeddedEventsMap = new ConcurrentDictionary<TEventSymbol, TEmbeddedEvent>(ReferenceEqualityComparer.Instance);

        private ImmutableArray<TEmbeddedType> _frozen;

        protected EmbeddedTypesManager(TPEModuleBuilder moduleBeingBuilt)
        {
            this.ModuleBeingBuilt = moduleBeingBuilt;
        }

        public override bool IsFrozen
        {
            get
            {
                return !_frozen.IsDefault;
            }
        }

        public override ImmutableArray<Cci.INamespaceTypeDefinition> GetTypes(DiagnosticBag diagnostics, HashSet<string> namesOfTopLevelTypes)
        {
            if (_frozen.IsDefault)
            {
                var builder = ArrayBuilder<TEmbeddedType>.GetInstance();
                builder.AddRange(EmbeddedTypesMap.Values);
                builder.Sort(TypeComparer.Instance);

                if (ImmutableInterlocked.InterlockedInitialize(ref _frozen, builder.ToImmutableAndFree()))
                {
                    if (_frozen.Length > 0)
                    {
                        Cci.INamespaceTypeDefinition prev = _frozen[0];
                        bool reportedDuplicate = HasNameConflict(namesOfTopLevelTypes, _frozen[0], diagnostics);

                        for (int i = 1; i < _frozen.Length; i++)
                        {
                            Cci.INamespaceTypeDefinition current = _frozen[i];

                            if (prev.NamespaceName == current.NamespaceName &&
                                prev.Name == current.Name)
                            {
                                if (!reportedDuplicate)
                                {
                                    Debug.Assert(_frozen[i - 1] == prev);

                                    // ERR_DuplicateLocalTypes3/ERR_InteropTypesWithSameNameAndGuid
                                    ReportNameCollisionBetweenEmbeddedTypes(_frozen[i - 1], _frozen[i], diagnostics);
                                    reportedDuplicate = true;
                                }
                            }
                            else
                            {
                                prev = current;
                                reportedDuplicate = HasNameConflict(namesOfTopLevelTypes, _frozen[i], diagnostics);
                            }
                        }

                        OnGetTypesCompleted(_frozen, diagnostics);
                    }
                }
            }

            return StaticCast<Cci.INamespaceTypeDefinition>.From(_frozen);
        }

        private bool HasNameConflict(HashSet<string> namesOfTopLevelTypes, TEmbeddedType type, DiagnosticBag diagnostics)
        {
            Cci.INamespaceTypeDefinition def = type;

            if (namesOfTopLevelTypes.Contains(MetadataHelpers.BuildQualifiedName(def.NamespaceName, def.Name)))
            {
                // ERR_LocalTypeNameClash2/ERR_LocalTypeNameClash
                ReportNameCollisionWithAlreadyDeclaredType(type, diagnostics);
                return true;
            }

            return false;
        }

        internal abstract int GetTargetAttributeSignatureIndex(TAttributeData attrData, AttributeDescription description);

        internal bool IsTargetAttribute(TAttributeData attrData, AttributeDescription description, out int signatureIndex)
        {
            signatureIndex = GetTargetAttributeSignatureIndex(attrData, description);
            return signatureIndex != -1;
        }

        internal abstract TAttributeData CreateSynthesizedAttribute(WellKnownMember constructor, ImmutableArray<TypedConstant> constructorArguments, ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract bool TryGetAttributeArguments(TAttributeData attrData, out ImmutableArray<TypedConstant> constructorArguments, out ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract void ReportIndirectReferencesToLinkedAssemblies(TAssemblySymbol assembly, DiagnosticBag diagnostics);

        protected abstract void OnGetTypesCompleted(ImmutableArray<TEmbeddedType> types, DiagnosticBag diagnostics);
        protected abstract void ReportNameCollisionBetweenEmbeddedTypes(TEmbeddedType typeA, TEmbeddedType typeB, DiagnosticBag diagnostics);
        protected abstract void ReportNameCollisionWithAlreadyDeclaredType(TEmbeddedType type, DiagnosticBag diagnostics);
        protected abstract TAttributeData CreateCompilerGeneratedAttribute();

        private sealed class TypeComparer : IComparer<TEmbeddedType>
        {
            public static readonly TypeComparer Instance = new TypeComparer();

            private TypeComparer()
            {
            }

            public int Compare(TEmbeddedType x, TEmbeddedType y)
            {
                Cci.INamespaceTypeDefinition dx = x;
                Cci.INamespaceTypeDefinition dy = y;

                int result = string.Compare(dx.NamespaceName, dy.NamespaceName, StringComparison.Ordinal);

                if (result == 0)
                {
                    result = string.Compare(dx.Name, dy.Name, StringComparison.Ordinal);

                    if (result == 0)
                    {
                        // this is a name conflict.
                        result = x.AssemblyRefIndex - y.AssemblyRefIndex;
                    }
                }

                return result;
            }
        }

        protected void EmbedReferences(Cci.ITypeDefinitionMember embeddedMember, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var noPiaIndexer = new Cci.TypeReferenceIndexer(new EmitContext(ModuleBeingBuilt, syntaxNodeOpt, diagnostics, metadataOnly: false, includePrivateMembers: true));
            noPiaIndexer.Visit(embeddedMember);
        }

        /// <summary>
        /// Returns null if member doesn't belong to an embedded NoPia type.
        /// </summary>
        protected abstract TEmbeddedType GetEmbeddedTypeForMember(TSymbol member, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);

        internal abstract TEmbeddedField EmbedField(TEmbeddedType type, TFieldSymbol field, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract TEmbeddedMethod EmbedMethod(TEmbeddedType type, TMethodSymbol method, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract TEmbeddedProperty EmbedProperty(TEmbeddedType type, TPropertySymbol property, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics);
        internal abstract TEmbeddedEvent EmbedEvent(TEmbeddedType type, TEventSymbol @event, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics, bool isUsedForComAwareEventBinding);

        internal Cci.IFieldReference EmbedFieldIfNeedTo(TFieldSymbol fieldSymbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            TEmbeddedType type = GetEmbeddedTypeForMember(fieldSymbol, syntaxNodeOpt, diagnostics);
            if (type != null)
            {
                return EmbedField(type, fieldSymbol, syntaxNodeOpt, diagnostics);
            }
            return fieldSymbol;
        }

        internal Cci.IMethodReference EmbedMethodIfNeedTo(TMethodSymbol methodSymbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            TEmbeddedType type = GetEmbeddedTypeForMember(methodSymbol, syntaxNodeOpt, diagnostics);
            if (type != null)
            {
                return EmbedMethod(type, methodSymbol, syntaxNodeOpt, diagnostics);
            }
            return methodSymbol;
        }

        internal void EmbedEventIfNeedTo(TEventSymbol eventSymbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics, bool isUsedForComAwareEventBinding)
        {
            TEmbeddedType type = GetEmbeddedTypeForMember(eventSymbol, syntaxNodeOpt, diagnostics);
            if (type != null)
            {
                EmbedEvent(type, eventSymbol, syntaxNodeOpt, diagnostics, isUsedForComAwareEventBinding);
            }
        }

        internal void EmbedPropertyIfNeedTo(TPropertySymbol propertySymbol, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            TEmbeddedType type = GetEmbeddedTypeForMember(propertySymbol, syntaxNodeOpt, diagnostics);
            if (type != null)
            {
                EmbedProperty(type, propertySymbol, syntaxNodeOpt, diagnostics);
            }
        }
    }
}
