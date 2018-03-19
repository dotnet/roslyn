// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class SymbolMatcher
    {
        public abstract Cci.ITypeReference MapReference(Cci.ITypeReference reference);
        public abstract Cci.IDefinition MapDefinition(Cci.IDefinition reference);

        public EmitBaseline MapBaselineToCompilation(
            EmitBaseline baseline,
            Compilation targetCompilation,
            CommonPEModuleBuilder targetModuleBuilder,
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> mappedSynthesizedMembers)
        {
            // Map all definitions to this compilation.
            IReadOnlyDictionary<Cci.ITypeDefinition, int> typesAdded = MapDefinitions(baseline.TypesAdded);
            IReadOnlyDictionary<Cci.IEventDefinition, int> eventsAdded = MapDefinitions(baseline.EventsAdded);
            IReadOnlyDictionary<Cci.IFieldDefinition, int> fieldsAdded = MapDefinitions(baseline.FieldsAdded);
            IReadOnlyDictionary<Cci.IMethodDefinition, int> methodsAdded = MapDefinitions(baseline.MethodsAdded);
            IReadOnlyDictionary<Cci.IPropertyDefinition, int> propertiesAdded = MapDefinitions(baseline.PropertiesAdded);

            return baseline.With(
                targetCompilation,
                targetModuleBuilder,
                baseline.Ordinal,
                baseline.EncId,
                typesAdded,
                eventsAdded,
                fieldsAdded,
                methodsAdded,
                propertiesAdded,
                eventMapAdded: baseline.EventMapAdded,
                propertyMapAdded: baseline.PropertyMapAdded,
                methodImplsAdded: baseline.MethodImplsAdded,
                tableEntriesAdded: baseline.TableEntriesAdded,
                blobStreamLengthAdded: baseline.BlobStreamLengthAdded,
                stringStreamLengthAdded: baseline.StringStreamLengthAdded,
                userStringStreamLengthAdded: baseline.UserStringStreamLengthAdded,
                guidStreamLengthAdded: baseline.GuidStreamLengthAdded,
                anonymousTypeMap: MapAnonymousTypes(baseline.AnonymousTypeMap),
                synthesizedMembers: mappedSynthesizedMembers,
                addedOrChangedMethods: MapAddedOrChangedMethods(baseline.AddedOrChangedMethods),
                debugInformationProvider: baseline.DebugInformationProvider, 
                localSignatureProvider: baseline.LocalSignatureProvider);
        }

        private IReadOnlyDictionary<K, V> MapDefinitions<K, V>(IReadOnlyDictionary<K, V> items)
            where K : Cci.IDefinition
        {
            var result = new Dictionary<K, V>();
            foreach (KeyValuePair<K, V> pair in items)
            {
                var key = (K)MapDefinition(pair.Key);

                // Result may be null if the definition was deleted, or if the definition
                // was synthesized (e.g.: an iterator type) and the method that generated
                // the synthesized definition was unchanged and not recompiled.
                if (key != null)
                {
                    result.Add(key, pair.Value);
                }
            }

            return result;
        }

        private IReadOnlyDictionary<int, AddedOrChangedMethodInfo> MapAddedOrChangedMethods(IReadOnlyDictionary<int, AddedOrChangedMethodInfo> addedOrChangedMethods)
        {
            var result = new Dictionary<int, AddedOrChangedMethodInfo>();

            foreach (KeyValuePair<int, AddedOrChangedMethodInfo> pair in addedOrChangedMethods)
            {
                result.Add(pair.Key, pair.Value.MapTypes(this));
            }

            return result;
        }

        private IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> MapAnonymousTypes(IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap)
        {
            var result = new Dictionary<AnonymousTypeKey, AnonymousTypeValue>();

            foreach (KeyValuePair<AnonymousTypeKey, AnonymousTypeValue> pair in anonymousTypeMap)
            {
                AnonymousTypeKey key = pair.Key;
                AnonymousTypeValue value = pair.Value;
                var type = (Cci.ITypeDefinition)MapDefinition(value.Type);
                Debug.Assert(type != null);
                result.Add(key, new AnonymousTypeValue(value.Name, value.UniqueIndex, type));
            }

            return result;
        }

        /// <summary>
        /// Merges synthesized members generated during lowering of the current compilation with aggregate synthesized members 
        /// from all previous source generations (gen >= 1).
        /// </summary>
        /// <remarks>
        /// Suppose {S -> {A, B, D}, T -> {E, F}} are all synthesized members in previous generations,
        /// and {S' -> {A', B', C}, U -> {G, H}} members are generated in the current compilation.
        /// 
        /// Where X matches X' via this matcher, i.e. X' is from the new compilation and 
        /// represents the same metadata entity as X in the previous compilation.
        /// 
        /// Then the resulting collection shall have the following entries:
        /// {S' -> {A', B', C, D}, U -> {G, H}, T -> {E, F}}
        /// </remarks>
        internal ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> MapSynthesizedMembers(
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> previousMembers,
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> newMembers)
        {
            // Note: we can't just return previous members if there are no new members, since we still need to map the symbols to the new compilation.

            if (previousMembers.Count == 0)
            {
                return newMembers;
            }

            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>>.Builder synthesizedMembersBuilder = ImmutableDictionary.CreateBuilder<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>>();

            synthesizedMembersBuilder.AddRange(newMembers);

            foreach (KeyValuePair<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> pair in previousMembers)
            {
                Cci.ITypeDefinition previousContainer = pair.Key;
                ImmutableArray<Cci.ITypeDefinitionMember> memberDefs = pair.Value;

                var mappedContainer = (Cci.ITypeDefinition)MapDefinition(previousContainer);
                if (mappedContainer == null)
                {
                    // No update to any member of the container type.  
                    synthesizedMembersBuilder.Add(previousContainer, memberDefs);
                    continue;
                }

                ImmutableArray<Cci.ITypeDefinitionMember> newSynthesizedMembers;
                if (!newMembers.TryGetValue(mappedContainer, out newSynthesizedMembers))
                {
                    // The container has been updated but the update didn't produce any synthesized members.
                    synthesizedMembersBuilder.Add(mappedContainer, memberDefs);
                    continue;
                }

                // The container has been updated and synthesized members produced.
                // They might be new or replacing existing ones. Merge existing with new.
                var memberBuilder = ArrayBuilder<Cci.ITypeDefinitionMember>.GetInstance();
                memberBuilder.AddRange(newSynthesizedMembers);

                foreach (Cci.ITypeDefinitionMember memberDef in memberDefs)
                {
                    var mappedMemberDef = (Cci.ITypeDefinitionMember)MapDefinition(memberDef);
                    if (mappedMemberDef != null)
                    {
                        // If the matcher found a member in the current compilation corresponding to previous memberDef,
                        // then the member has to be synthesized and produced as a result of a method update 
                        // and thus already contained in newSynthesizedMembers.
                        Debug.Assert(newSynthesizedMembers.Contains(mappedMemberDef));
                    }
                    else
                    {
                        memberBuilder.Add(memberDef);
                    }
                }

                synthesizedMembersBuilder[mappedContainer] = memberBuilder.ToImmutableAndFree();
            }

            return synthesizedMembersBuilder.ToImmutable();
        }
    }
}
