// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        internal override bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PENamedTypeSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(TypeHandle);
                return false;
            }
        }

        internal override bool TryGetEventHandle(Cci.IEventDefinition def, out EventHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEEventSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(EventHandle);
                return false;
            }
        }

        internal override bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEFieldSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(FieldHandle);
                return false;
            }
        }

        internal override bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEMethodSymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(MethodHandle);
                return false;
            }
        }

        private bool TryGetMethodHandle(EmitBaseline baseline, Cci.IMethodDefinition def, out MethodHandle handle)
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
                    handle = MetadataTokens.MethodHandle((int)methodIndex);
                    return true;
                }
            }

            handle = default(MethodHandle);
            return false;
        }

        internal override bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyHandle handle)
        {
            var other = this.mapToMetadata.MapDefinition(def) as PEPropertySymbol;
            if ((object)other != null)
            {
                handle = other.Handle;
                return true;
            }
            else
            {
                handle = default(PropertyHandle);
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
            MethodHandle handle;
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
                // Not necessary to preserve locals.
                return null;
            }

            CSharpSymbolMatcher symbolMap;
            ImmutableArray<EncLocalInfo> previousLocals;

            uint methodIndex = (uint)MetadataTokens.GetRowNumber(handle);

            // Check if method has changed previously. If so, we already have a map.
            if (baseline.LocalsForMethodsAddedOrChanged.TryGetValue(methodIndex, out previousLocals))
            {
                symbolMap = this.mapToPrevious;
            }
            else
            {
                // Method has not changed since initial generation. Generate a map
                // using the local names provided with the initial metadata.

                ImmutableArray<MetadataDecoder.LocalInfo> slotMetadata;
                if (!metadataDecoder.TryGetLocals(handle, out slotMetadata))
                {
                    // TODO: Report error that metadata is not supported.
                    return null;
                }

                var debugInfo = baseline.DebugInformationProvider(handle);

                previousLocals = CreateLocalSlotMap(debugInfo, slotMetadata);
                Debug.Assert(previousLocals.Length == slotMetadata.Length);
                symbolMap = this.mapToMetadata;
            }

            return new EncVariableSlotAllocator(symbolMap, methodEntry.SyntaxMap, methodEntry.PreviousMethod, previousLocals);
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
                    ValueTuple<SynthesizedLocalKind, LocalDebugId> slot = localSlots[slotIndex];
                    if (slot.Item1.IsLongLived())
                    {
                        var metadata = slotMetadata[slotIndex];

                        // We do not emit custom modifiers on locals so ignore the
                        // previous version of the local if it had custom modifiers.
                        if (metadata.CustomModifiers.IsDefaultOrEmpty)
                        {
                            var local = new EncLocalInfo(slot.Item2, (Cci.ITypeReference)metadata.Type, metadata.Constraints, slot.Item1, metadata.SignatureOpt);
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
