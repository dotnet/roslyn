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

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Matches symbols from an assembly in one compilation to
    /// the corresponding assembly in another. Assumes that only
    /// one assembly has changed between the two compilations.
    /// </summary>
    internal sealed partial class CSharpDefinitionMap : DefinitionMap<CSharpSymbolMatcher>
    {
        private readonly MetadataDecoder _metadataDecoder;

        public CSharpDefinitionMap(
            PEModule module,
            IEnumerable<SemanticEdit> edits,
            MetadataDecoder metadataDecoder,
            CSharpSymbolMatcher mapToMetadata,
            CSharpSymbolMatcher mapToPrevious)
            : base(module, edits, mapToMetadata, mapToPrevious)
        {
            Debug.Assert(metadataDecoder != null);
            _metadataDecoder = metadataDecoder;
        }

        internal override CommonMessageProvider MessageProvider => CSharp.MessageProvider.Instance;

        internal bool TryGetAnonymousTypeName(NamedTypeSymbol template, out string name, out int index)
        {
            return this.mapToPrevious.TryGetAnonymousTypeName(template, out name, out index);
        }

        internal override bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeDefinitionHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PENamedTypeSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(TypeDefinitionHandle);
                return false;
            }
        }

        internal override bool TryGetEventHandle(Cci.IEventDefinition def, out EventDefinitionHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEEventSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(EventDefinitionHandle);
                return false;
            }
        }

        internal override bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldDefinitionHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEFieldSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(FieldDefinitionHandle);
                return false;
            }
        }

        internal override bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodDefinitionHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEMethodSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(MethodDefinitionHandle);
                return false;
            }
        }

        internal override bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyDefinitionHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEPropertySymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(PropertyDefinitionHandle);
                return false;
            }
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

        protected override ImmutableArray<EncLocalInfo> TryGetLocalSlotMapFromMetadata(MethodDefinitionHandle handle, EditAndContinueMethodDebugInformation debugInfo)
        {
            ImmutableArray<LocalInfo<TypeSymbol>> slotMetadata;
            if (!_metadataDecoder.TryGetLocals(handle, out slotMetadata))
            {
                return default(ImmutableArray<EncLocalInfo>);
            }

            var result = CreateLocalSlotMap(debugInfo, slotMetadata);
            Debug.Assert(result.Length == slotMetadata.Length);
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
