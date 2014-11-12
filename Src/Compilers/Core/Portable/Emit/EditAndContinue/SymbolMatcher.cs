// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> targetCompilationSynthesizedMembers)
        {
            // Map all definitions to this compilation.
            var typesAdded = MapDefinitions(baseline.TypesAdded);
            var eventsAdded = MapDefinitions(baseline.EventsAdded);
            var fieldsAdded = MapDefinitions(baseline.FieldsAdded);
            var methodsAdded = MapDefinitions(baseline.MethodsAdded);
            var propertiesAdded = MapDefinitions(baseline.PropertiesAdded);

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
                synthesizedMembers: MapSynthesizedMembers(baseline.SynthesizedMembers, targetCompilationSynthesizedMembers),
                addedOrChangedMethods: MapAddedOrChangedMethods(baseline.AddedOrChangedMethods),
                debugInformationProvider: baseline.DebugInformationProvider);
        }

        private IReadOnlyDictionary<K, V> MapDefinitions<K, V>(IReadOnlyDictionary<K, V> items)
            where K : Cci.IDefinition
        {
            var result = new Dictionary<K, V>();
            foreach (var pair in items)
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

        private IReadOnlyDictionary<uint, AddedOrChangedMethodInfo> MapAddedOrChangedMethods(IReadOnlyDictionary<uint, AddedOrChangedMethodInfo> addedOrChangedMethods)
        {
            var result = new Dictionary<uint, AddedOrChangedMethodInfo>();

            foreach (var pair in addedOrChangedMethods)
            {
                result.Add(pair.Key, pair.Value.MapTypes(this));
            }

            return result;
        }

        private IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> MapAnonymousTypes(IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap)
        {
            var result = new Dictionary<AnonymousTypeKey, AnonymousTypeValue>();

            foreach (var pair in anonymousTypeMap)
            {
                var key = pair.Key;
                var value = pair.Value;
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
        private ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> MapSynthesizedMembers(
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> previousMembers,
            ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> newMembers)
        {
            var synthesizedMembersBuilder = ImmutableDictionary.CreateBuilder<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>>();

            synthesizedMembersBuilder.AddRange(newMembers);

            foreach (var pair in previousMembers)
            {
                var typeDef = pair.Key;
                var memberDefs = pair.Value;
                var mappedTypeDef = (Cci.ITypeDefinition)MapDefinition(typeDef);

                if (mappedTypeDef != null)
                {
                    // If the matcher found a type in the current compilation corresponding to typeDef,
                    // the type def has to be contained in newMembers.
                    Debug.Assert(newMembers.ContainsKey(mappedTypeDef));

                    var memberBuilder = ArrayBuilder<Cci.ITypeDefinitionMember>.GetInstance();

                    // add existing members:
                    memberBuilder.AddRange(newMembers[mappedTypeDef]);

                    foreach (var memberDef in memberDefs)
                    {
                        var mappedMemberDef = (Cci.ITypeDefinitionMember)MapDefinition(memberDef);
                        if (mappedMemberDef != null)
                        {
                            // If the matcher found a member in the current compilation corresponding to memberDef,
                            // the member def has to be contained in newMembers.
                            Debug.Assert(newMembers[mappedTypeDef].Contains(mappedMemberDef));
                        }
                        else
                        {
                            memberBuilder.Add(memberDef);
                        }
                    }

                    // the type must already be present, update its list of members:
                    synthesizedMembersBuilder[mappedTypeDef] = memberBuilder.ToImmutableAndFree();
                }
                else
                {
                    synthesizedMembersBuilder.Add(typeDef, memberDefs);
                }
            }

            return synthesizedMembersBuilder.ToImmutable();
        }
    }
}
