// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Matches symbols from an assembly in one compilation to
    /// the corresponding assembly in another. Assumes that only
    /// one assembly has changed between the two compilations.
    /// </summary>
    internal sealed partial class CSharpDefinitionMap : DefinitionMap
    {
        private readonly MetadataDecoder _metadataDecoder;

        public CSharpDefinitionMap(
            IEnumerable<SemanticEdit> edits,
            MetadataDecoder metadataDecoder,
            CSharpSymbolMatcher mapToMetadata,
            CSharpSymbolMatcher mapToPrevious)
            : base(edits, mapToMetadata, mapToPrevious)
        {
            Debug.Assert(metadataDecoder != null);
            _metadataDecoder = metadataDecoder;
        }

        internal override CommonMessageProvider MessageProvider
            => CSharp.MessageProvider.Instance;

        protected override LambdaSyntaxFacts GetLambdaSyntaxFacts()
            => CSharpLambdaSyntaxFacts.Instance;

        internal bool TryGetAnonymousTypeName(IAnonymousTypeTemplateSymbolInternal template, out string name, out int index)
            => mapToPrevious.TryGetAnonymousTypeName(template, out name, out index);

        internal override bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeDefinitionHandle handle)
        {
            if (mapToMetadata.MapDefinition(def) is PENamedTypeSymbol other)
            {
                handle = other.Handle;
                return true;
            }

            handle = default;
            return false;
        }

        internal override bool TryGetEventHandle(Cci.IEventDefinition def, out EventDefinitionHandle handle)
        {
            if (mapToMetadata.MapDefinition(def) is PEEventSymbol other)
            {
                handle = other.Handle;
                return true;
            }

            handle = default;
            return false;
        }

        internal override bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldDefinitionHandle handle)
        {
            if (mapToMetadata.MapDefinition(def) is PEFieldSymbol other)
            {
                handle = other.Handle;
                return true;
            }

            handle = default;
            return false;
        }

        internal override bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodDefinitionHandle handle)
        {
            if (mapToMetadata.MapDefinition(def) is PEMethodSymbol other)
            {
                handle = other.Handle;
                return true;
            }

            handle = default;
            return false;
        }

        internal override bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyDefinitionHandle handle)
        {
            if (mapToMetadata.MapDefinition(def) is PEPropertySymbol other)
            {
                handle = other.Handle;
                return true;
            }

            handle = default;
            return false;
        }

        protected override void GetStateMachineFieldMapFromMetadata(
            ITypeSymbol stateMachineType,
            ImmutableArray<LocalSlotDebugInfo> localSlotDebugInfo,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap,
            out int awaiterSlotCount)
        {
            // we are working with PE symbols
            Debug.Assert(stateMachineType.ContainingAssembly is PEAssemblySymbol);

            var hoistedLocals = new Dictionary<EncHoistedLocalInfo, int>();
            var awaiters = new Dictionary<Cci.ITypeReference, int>();
            int maxAwaiterSlotIndex = -1;

            foreach (var member in stateMachineType.GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                {
                    string name = member.Name;
                    int slotIndex;

                    switch (GeneratedNames.GetKind(name))
                    {
                        case GeneratedNameKind.AwaiterField:
                            if (GeneratedNames.TryParseSlotIndex(name, out slotIndex))
                            {
                                var field = (IFieldSymbol)member;

                                // correct metadata won't contain duplicates, but malformed might, ignore the duplicate:
                                awaiters[(Cci.ITypeReference)field.Type] = slotIndex;

                                if (slotIndex > maxAwaiterSlotIndex)
                                {
                                    maxAwaiterSlotIndex = slotIndex;
                                }
                            }

                            break;

                        case GeneratedNameKind.HoistedLocalField:
                        case GeneratedNameKind.HoistedSynthesizedLocalField:
                            if (GeneratedNames.TryParseSlotIndex(name, out slotIndex))
                            {
                                var field = (IFieldSymbol)member;
                                if (slotIndex >= localSlotDebugInfo.Length)
                                {
                                    // invalid or missing metadata
                                    continue;
                                }

                                var key = new EncHoistedLocalInfo(localSlotDebugInfo[slotIndex], (Cci.ITypeReference)field.Type);

                                // correct metadata won't contain duplicate ids, but malformed might, ignore the duplicate:
                                hoistedLocals[key] = slotIndex;
                            }

                            break;
                    }
                }
            }

            hoistedLocalMap = hoistedLocals;
            awaiterMap = awaiters;
            awaiterSlotCount = maxAwaiterSlotIndex + 1;
        }

        protected override ImmutableArray<EncLocalInfo> GetLocalSlotMapFromMetadata(StandaloneSignatureHandle handle, EditAndContinueMethodDebugInformation debugInfo)
        {
            Debug.Assert(!handle.IsNil);

            var localInfos = _metadataDecoder.GetLocalsOrThrow(handle);
            var result = CreateLocalSlotMap(debugInfo, localInfos);
            Debug.Assert(result.Length == localInfos.Length);
            return result;
        }

        protected override ITypeSymbol TryGetStateMachineType(EntityHandle methodHandle)
        {
            string typeName;
            if (_metadataDecoder.Module.HasStringValuedAttribute(methodHandle, AttributeDescription.AsyncStateMachineAttribute, out typeName) ||
                _metadataDecoder.Module.HasStringValuedAttribute(methodHandle, AttributeDescription.IteratorStateMachineAttribute, out typeName))
            {
                return _metadataDecoder.GetTypeSymbolForSerializedType(typeName);
            }

            return null;
        }

        /// <summary>
        /// Match local declarations to names to generate a map from
        /// declaration to local slot. The names are indexed by slot and the
        /// assumption is that declarations are in the same order as slots.
        /// </summary>
        private static ImmutableArray<EncLocalInfo> CreateLocalSlotMap(
            EditAndContinueMethodDebugInformation methodEncInfo,
            ImmutableArray<LocalInfo<TypeSymbol>> slotMetadata)
        {
            var result = new EncLocalInfo[slotMetadata.Length];

            var localSlots = methodEncInfo.LocalSlots;
            if (!localSlots.IsDefault)
            {
                // In case of corrupted PDB or metadata, these lengths might not match.
                // Let's guard against such case.
                int slotCount = Math.Min(localSlots.Length, slotMetadata.Length);

                var map = new Dictionary<EncLocalInfo, int>();

                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    var slot = localSlots[slotIndex];
                    if (slot.SynthesizedKind.IsLongLived())
                    {
                        var metadata = slotMetadata[slotIndex];

                        // We do not emit custom modifiers on locals so ignore the
                        // previous version of the local if it had custom modifiers.
                        if (metadata.CustomModifiers.IsDefaultOrEmpty)
                        {
                            var local = new EncLocalInfo(slot, (Cci.ITypeReference)metadata.Type, metadata.Constraints, metadata.SignatureOpt);
                            map.Add(local, slotIndex);
                        }
                    }
                }

                foreach (var pair in map)
                {
                    result[pair.Value] = pair.Key;
                }
            }

            // Populate any remaining locals that were not matched to source.
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i].IsDefault)
                {
                    result[i] = new EncLocalInfo(slotMetadata[i].SignatureOpt);
                }
            }

            return ImmutableArray.Create(result);
        }
    }
}
