// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    /// <summary>
    /// Matches symbols from an assembly in one compilation to
    /// the corresponding assembly in another. Assumes that only
    /// one assembly has changed between the two compilations.
    /// </summary>
    internal sealed partial class CSharpDefinitionMap : DefinitionMap
    {
        private readonly MetadataDecoder metadataDecoder;
        private readonly CSharpSymbolMatcher mapToMetadata;
        private readonly CSharpSymbolMatcher mapToPrevious;

        public CSharpDefinitionMap(
            PEModule module,
            IEnumerable<SemanticEdit> edits,
            MetadataDecoder metadataDecoder,
            CSharpSymbolMatcher mapToMetadata,
            CSharpSymbolMatcher mapToPrevious)
            : base(module, edits)
        {
            Debug.Assert(mapToMetadata != null);
            Debug.Assert(metadataDecoder != null);

            this.mapToMetadata = mapToMetadata;
            this.mapToPrevious = mapToPrevious ?? mapToMetadata;
            this.metadataDecoder = metadataDecoder;
        }

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

        private bool TryGetMethodHandle(EmitBaseline baseline, Cci.IMethodDefinition def, out MethodDefinitionHandle handle)
        {
            if (this.TryGetMethodHandle(def, out handle))
            {
                return true;
            }

            def = (Cci.IMethodDefinition)this.mapToPrevious.MapDefinition(def);
            if (def != null)
            {
                uint methodIndex;
                if (baseline.MethodsAdded.TryGetValue(def, out methodIndex))
                {
                    handle = MetadataTokens.MethodDefinitionHandle((int)methodIndex);
                    return true;
                }
            }

            handle = default(MethodDefinitionHandle);
            return false;
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

        internal override bool DefinitionExists(Cci.IDefinition def)
        {
            var previous = this.mapToPrevious.MapDefinition(def);
            return previous != null;
        }

        internal override VariableSlotAllocator TryCreateVariableSlotAllocator(EmitBaseline baseline, IMethodSymbol method)
        {
            MethodDefinitionHandle handle;
            if (!this.TryGetMethodHandle(baseline, (Cci.IMethodDefinition)method, out handle))
            {
                // Unrecognized method. Must have been added in the current compilation.
                return null;
            }

            MethodDefinitionEntry methodEntry;
            if (!this.methodMap.TryGetValue(method, out methodEntry))
            {
                // Not part of changeset. No need to preserve locals.
                return null;
            }

            if (!methodEntry.PreserveLocalVariables)
            {
                // We should always "preserve locals" of iterator and async methods since the state machine 
                // might be active without MoveNext method being on stack. We don't enforce this requirement here,
                // since a method may be incorrectly marked by Iterator/AsyncStateMachine attribute by the user, 
                // in which case we can't reliably figure out that it's an error in semantic edit set. 

                return null;
            }

            CSharpSymbolMatcher symbolMap;
            ImmutableArray<EncLocalInfo> previousLocals;
            IReadOnlyDictionary<EncLocalInfo, string> previousHoistedLocalMap;
            IReadOnlyDictionary<Cci.ITypeReference, string> awaiterMap;
            int hoistedLocalSlotCount;
            int awaiterSlotCount;
            string previousStateMachineTypeNameOpt;

            uint methodIndex = (uint)MetadataTokens.GetRowNumber(handle);

            // Check if method has changed previously. If so, we already have a map.
            if (baseline.LocalsForMethodsAddedOrChanged.TryGetValue(methodIndex, out previousLocals))
            {
                symbolMap = this.mapToPrevious;

                // TODO:
                previousHoistedLocalMap = null;
                awaiterMap = null;
                hoistedLocalSlotCount = 0;
                awaiterSlotCount = 0;
                previousStateMachineTypeNameOpt = null;
            }
            else
            {
                // Method has not changed since initial generation. Generate a map
                // using the local names provided with the initial metadata.

                var debugInfo = baseline.DebugInformationProvider(handle);
                TypeSymbol stateMachineType = TryGetStateMachineType(handle);
                if (stateMachineType != null)
                {
                    var localSlotDebugInfo = debugInfo.LocalSlots.NullToEmpty();

                    // method is async/iterator kickoff method
                    GetStateMachineFieldMap(stateMachineType, localSlotDebugInfo, out previousHoistedLocalMap, out awaiterMap, out awaiterSlotCount);

                    // Kickoff method has no interesting locals on its own. 
                    // We use the EnC method debug infromation for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    hoistedLocalSlotCount = localSlotDebugInfo.Length;
                    previousStateMachineTypeNameOpt = stateMachineType.Name;
                }
                else
                {
                    ImmutableArray<MetadataDecoder.LocalInfo> slotMetadata;
                    if (!metadataDecoder.TryGetLocals(handle, out slotMetadata))
                    {
                        // TODO: Report error that metadata is not supported.
                        return null;
                    }

                    previousLocals = CreateLocalSlotMap(debugInfo, slotMetadata);
                    Debug.Assert(previousLocals.Length == slotMetadata.Length);

                    hoistedLocalSlotCount = 0;
                    previousHoistedLocalMap = null;
                    awaiterMap = null;
                    awaiterSlotCount = 0;
                    previousStateMachineTypeNameOpt = null;
                }

                symbolMap = this.mapToMetadata;
            }

            return new EncVariableSlotAllocator(
                symbolMap,
                methodEntry.SyntaxMap, 
                methodEntry.PreviousMethod, 
                previousLocals, 
                previousStateMachineTypeNameOpt,
                hoistedLocalSlotCount, 
                previousHoistedLocalMap, 
                awaiterSlotCount,
                awaiterMap);
        }

        private void GetStateMachineFieldMap(
            TypeSymbol stateMachineType,
            ImmutableArray<LocalSlotDebugInfo> localSlotDebugInfo,
            out IReadOnlyDictionary<EncLocalInfo, string> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, string> awaiterMap,
            out int awaiterSlotCount)
        {
            var hoistedLocals = new Dictionary<EncLocalInfo, string>();
            var awaiters = new Dictionary<Cci.ITypeReference, string>();
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
                                var field = (FieldSymbol)member;

                                // correct metadata won't contain duplicates, but malformed might, ignore the duplicate:
                                awaiters[(Cci.ITypeReference)field.Type] = name;

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
                                var field = (FieldSymbol)member;
                                if (slotIndex >= localSlotDebugInfo.Length)
                                {
                                    // invalid or missing metadata
                                    continue;
                                }

                                var key = new EncLocalInfo(
                                    localSlotDebugInfo[slotIndex].Id,
                                    (Cci.ITypeReference)field.Type,
                                    LocalSlotConstraints.None,
                                    localSlotDebugInfo[slotIndex].SynthesizedKind,
                                    signature: null);

                                // correct metadata won't contain duplicate ids, but malformed might, ignore the duplicate:
                                hoistedLocals[key] = name;
                            }

                            break;
                    }
                }
            }

            hoistedLocalMap = hoistedLocals;
            awaiterMap = awaiters;
            awaiterSlotCount = maxAwaiterSlotIndex + 1;
        }

        private TypeSymbol TryGetStateMachineType(Handle methodHandle)
        {
            string typeName;
            if (metadataDecoder.Module.HasStringValuedAttribute(methodHandle, AttributeDescription.AsyncStateMachineAttribute, out typeName) ||
                metadataDecoder.Module.HasStringValuedAttribute(methodHandle, AttributeDescription.IteratorStateMachineAttribute, out typeName))
            {
                return metadataDecoder.GetTypeSymbolForSerializedType(typeName);
            }

            return null;
        }

        /// <summary>
        /// Match local declarations to names to generate a map from
        /// declaration to local slot. The names are indexed by slot and the
        /// assumption is that declarations are in the same order as slots.
        /// </summary>
        public static ImmutableArray<EncLocalInfo> CreateLocalSlotMap(
            EditAndContinueMethodDebugInformation methodEncInfo,
            ImmutableArray<MetadataDecoder.LocalInfo> slotMetadata)
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
                            var local = new EncLocalInfo(slot.Id, (Cci.ITypeReference)metadata.Type, metadata.Constraints, slot.SynthesizedKind, metadata.SignatureOpt);
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
