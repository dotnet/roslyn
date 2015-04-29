// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class DefinitionMap
    {
        protected struct MethodUpdate
        {
            public MethodUpdate(IMethodSymbolInternal previousMethod, bool preserveLocalVariables, Func<SyntaxNode, SyntaxNode> syntaxMap)
            {
                this.PreviousMethod = previousMethod;
                this.PreserveLocalVariables = preserveLocalVariables;
                this.SyntaxMap = syntaxMap;
            }

            public readonly IMethodSymbolInternal PreviousMethod;
            public readonly bool PreserveLocalVariables;
            public readonly Func<SyntaxNode, SyntaxNode> SyntaxMap;
        }

        protected readonly PEModule module;
        protected readonly IReadOnlyDictionary<IMethodSymbol, MethodUpdate> methodUpdates;

        protected DefinitionMap(PEModule module, IEnumerable<SemanticEdit> edits)
        {
            Debug.Assert(module != null);
            Debug.Assert(edits != null);

            this.module = module;
            this.methodUpdates = GetMethodUpdates(edits);
        }

        private static IReadOnlyDictionary<IMethodSymbol, MethodUpdate> GetMethodUpdates(IEnumerable<SemanticEdit> edits)
        {
            var methodUpdates = new Dictionary<IMethodSymbol, MethodUpdate>();
            foreach (var edit in edits)
            {
                if (edit.Kind == SemanticEditKind.Update)
                {
                    var method = edit.NewSymbol as IMethodSymbol;
                    if (method != null)
                    {
                        methodUpdates.Add(method, new MethodUpdate(
                            (IMethodSymbolInternal)edit.OldSymbol,
                            edit.PreserveLocalVariables,
                            edit.SyntaxMap));
                    }
                }
            }

            return methodUpdates;
        }

        internal abstract Cci.IDefinition MapDefinition(Cci.IDefinition definition);

        internal bool DefinitionExists(Cci.IDefinition definition)
        {
            return MapDefinition(definition) != null;
        }

        internal abstract bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeDefinitionHandle handle);
        internal abstract bool TryGetEventHandle(Cci.IEventDefinition def, out EventDefinitionHandle handle);
        internal abstract bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldDefinitionHandle handle);
        internal abstract bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodDefinitionHandle handle);
        internal abstract bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyDefinitionHandle handle);
    }

    internal abstract class DefinitionMap<TSymbolMatcher> : DefinitionMap
        where TSymbolMatcher : SymbolMatcher
    {
        protected readonly TSymbolMatcher mapToMetadata;
        protected readonly TSymbolMatcher mapToPrevious;

        protected DefinitionMap(PEModule module, IEnumerable<SemanticEdit> edits, TSymbolMatcher mapToMetadata, TSymbolMatcher mapToPrevious)
            : base(module, edits)
        {
            Debug.Assert(mapToMetadata != null);

            this.mapToMetadata = mapToMetadata;
            this.mapToPrevious = mapToPrevious ?? mapToMetadata;
        }

        internal sealed override Cci.IDefinition MapDefinition(Cci.IDefinition definition)
        {
            return this.mapToPrevious.MapDefinition(definition) ??
                   (this.mapToMetadata != this.mapToPrevious ? this.mapToMetadata.MapDefinition(definition) : null);
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

        protected static IReadOnlyDictionary<SyntaxNode, int> CreateDeclaratorToSyntaxOrdinalMap(ImmutableArray<SyntaxNode> declarators)
        {
            var declaratorToIndex = new Dictionary<SyntaxNode, int>();
            for (int i = 0; i < declarators.Length; i++)
            {
                declaratorToIndex.Add(declarators[i], i);
            }

            return declaratorToIndex;
        }

        protected abstract void GetStateMachineFieldMapFromMetadata(
            ITypeSymbol stateMachineType,
            ImmutableArray<LocalSlotDebugInfo> localSlotDebugInfo,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap,
            out int awaiterSlotCount);

        protected abstract ImmutableArray<EncLocalInfo> TryGetLocalSlotMapFromMetadata(MethodDefinitionHandle handle, EditAndContinueMethodDebugInformation debugInfo);
        protected abstract ITypeSymbol TryGetStateMachineType(Handle methodHandle);

        internal VariableSlotAllocator TryCreateVariableSlotAllocator(EmitBaseline baseline, IMethodSymbol method)
        {
            MethodDefinitionHandle handle;
            if (!this.TryGetMethodHandle(baseline, (Cci.IMethodDefinition)method, out handle))
            {
                // Unrecognized method. Must have been added in the current compilation.
                return null;
            }

            MethodUpdate methodUpdate;
            if (!this.methodUpdates.TryGetValue(method, out methodUpdate))
            {
                // Not part of changeset. No need to preserve locals.
                return null;
            }

            if (!methodUpdate.PreserveLocalVariables)
            {
                // We should always "preserve locals" of iterator and async methods since the state machine 
                // might be active without MoveNext method being on stack. We don't enforce this requirement here,
                // since a method may be incorrectly marked by Iterator/AsyncStateMachine attribute by the user, 
                // in which case we can't reliably figure out that it's an error in semantic edit set. 

                // We should also "preserve locals" of any updated method containing lambdas. The goal is to 
                // treat lambdas the same as method declarations. Lambdas declared in a method body that escape 
                // the method (are assigned to a field, added to an event, e.g.) might be invoked after the method 
                // is updated and when it no longer contains active statements. If we didn't map the lambdas of 
                // the updated body to the original lambdas we would run the out-of-date lambda bodies, 
                // which would not happen if the lambdas were named methods.
                return null;
            }

            ImmutableArray<EncLocalInfo> previousLocals;
            IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap = null;
            IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap = null;
            IReadOnlyDictionary<int, KeyValuePair<DebugId, int>> lambdaMap = null;
            IReadOnlyDictionary<int, DebugId> closureMap = null;

            int hoistedLocalSlotCount = 0;
            int awaiterSlotCount = 0;
            string stateMachineTypeNameOpt = null;
            TSymbolMatcher symbolMap;

            uint methodIndex = (uint)MetadataTokens.GetRowNumber(handle);
            DebugId methodId;

            // Check if method has changed previously. If so, we already have a map.
            AddedOrChangedMethodInfo addedOrChangedMethod;
            if (baseline.AddedOrChangedMethods.TryGetValue(methodIndex, out addedOrChangedMethod))
            {
                methodId = addedOrChangedMethod.MethodId;

                MakeLambdaAndClosureMaps(addedOrChangedMethod.LambdaDebugInfo, addedOrChangedMethod.ClosureDebugInfo, out lambdaMap, out closureMap);

                if (addedOrChangedMethod.StateMachineTypeNameOpt != null)
                {
                    // method is async/iterator kickoff method
                    GetStateMachineFieldMapFromPreviousCompilation(
                        addedOrChangedMethod.StateMachineHoistedLocalSlotsOpt,
                        addedOrChangedMethod.StateMachineAwaiterSlotsOpt,
                        out hoistedLocalMap,
                        out awaiterMap);

                    hoistedLocalSlotCount = addedOrChangedMethod.StateMachineHoistedLocalSlotsOpt.Length;
                    awaiterSlotCount = addedOrChangedMethod.StateMachineAwaiterSlotsOpt.Length;

                    // Kickoff method has no interesting locals on its own. 
                    // We use the EnC method debug infromation for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    stateMachineTypeNameOpt = addedOrChangedMethod.StateMachineTypeNameOpt;
                }
                else
                {
                    previousLocals = addedOrChangedMethod.Locals;
                }

                // All types that AddedOrChangedMethodInfo refers to have been mapped to the previous generation.
                // Therefore we don't need to fall back to metadata if we don't find the type reference, like we do in DefinitionMap.MapReference.
                symbolMap = mapToPrevious;
            }
            else
            {
                // Method has not changed since initial generation. Generate a map
                // using the local names provided with the initial metadata.
                var debugInfo = baseline.DebugInformationProvider(handle);

                methodId = new DebugId(debugInfo.MethodOrdinal, 0);

                if (!debugInfo.Lambdas.IsDefaultOrEmpty)
                {
                    MakeLambdaAndClosureMaps(debugInfo.Lambdas, debugInfo.Closures, out lambdaMap, out closureMap);
                }

                ITypeSymbol stateMachineType = TryGetStateMachineType(handle);
                if (stateMachineType != null)
                {
                    // method is async/iterator kickoff method
                    var localSlotDebugInfo = debugInfo.LocalSlots.NullToEmpty();
                    GetStateMachineFieldMapFromMetadata(stateMachineType, localSlotDebugInfo, out hoistedLocalMap, out awaiterMap, out awaiterSlotCount);
                    hoistedLocalSlotCount = localSlotDebugInfo.Length;

                    // Kickoff method has no interesting locals on its own. 
                    // We use the EnC method debug infromation for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    stateMachineTypeNameOpt = stateMachineType.Name;
                }
                else
                {
                    previousLocals = TryGetLocalSlotMapFromMetadata(handle, debugInfo);
                    if (previousLocals.IsDefault)
                    {
                        // TODO: Report error that metadata is not supported.
                        return null;
                    }
                }

                symbolMap = mapToMetadata;
            }

            return new EncVariableSlotAllocator(
                symbolMap,
                methodUpdate.SyntaxMap,
                methodUpdate.PreviousMethod,
                methodId,
                previousLocals,
                lambdaMap,
                closureMap,
                stateMachineTypeNameOpt,
                hoistedLocalSlotCount,
                hoistedLocalMap,
                awaiterSlotCount,
                awaiterMap);
        }

        private static void MakeLambdaAndClosureMaps(
            ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            out IReadOnlyDictionary<int, KeyValuePair<DebugId, int>> lambdaMap,
            out IReadOnlyDictionary<int, DebugId> closureMap)
        {
            var lambdas = new Dictionary<int, KeyValuePair<DebugId, int>>(lambdaDebugInfo.Length);
            var closures = new Dictionary<int, DebugId>(closureDebugInfo.Length);

            for (int i = 0; i < lambdaDebugInfo.Length; i++)
            {
                var lambdaInfo = lambdaDebugInfo[i];
                lambdas[lambdaInfo.SyntaxOffset] = KeyValuePair.Create(lambdaInfo.LambdaId, lambdaInfo.ClosureOrdinal);
            }

            for (int i = 0; i < closureDebugInfo.Length; i++)
            {
                var closureInfo = closureDebugInfo[i];
                closures[closureInfo.SyntaxOffset] = closureInfo.ClosureId;
            }

            lambdaMap = lambdas;
            closureMap = closures;
        }

        private static void GetStateMachineFieldMapFromPreviousCompilation(
            ImmutableArray<EncHoistedLocalInfo> hoistedLocalSlots,
            ImmutableArray<Cci.ITypeReference> hoistedAwaiters,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap)
        {
            var hoistedLocals = new Dictionary<EncHoistedLocalInfo, int>();
            var awaiters = new Dictionary<Cci.ITypeReference, int>();

            for (int slotIndex = 0; slotIndex < hoistedLocalSlots.Length; slotIndex++)
            {
                var slot = hoistedLocalSlots[slotIndex];
                if (slot.IsUnused)
                {
                    // Unused field.
                    continue;
                }

                hoistedLocals.Add(slot, slotIndex);
            }

            for (int slotIndex = 0; slotIndex < hoistedAwaiters.Length; slotIndex++)
            {
                var slot = hoistedAwaiters[slotIndex];
                if (slot == null)
                {
                    // Unused awaiter.
                    continue;
                }

                awaiters.Add(slot, slotIndex);
            }

            hoistedLocalMap = hoistedLocals;
            awaiterMap = awaiters;
        }
    }
}
