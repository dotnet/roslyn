// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class DefinitionMap
    {
        protected readonly struct MappedMethod
        {
            public readonly IMethodSymbolInternal PreviousMethod;
            public readonly Func<SyntaxNode, SyntaxNode?>? SyntaxMap;

            public MappedMethod(IMethodSymbolInternal previousMethod, Func<SyntaxNode, SyntaxNode?>? syntaxMap)
            {
                PreviousMethod = previousMethod;
                SyntaxMap = syntaxMap;
            }
        }

        private readonly ImmutableDictionary<IMethodSymbolInternal, MethodInstrumentation> _methodInstrumentations;
        protected readonly IReadOnlyDictionary<IMethodSymbolInternal, MappedMethod> mappedMethods;
        public readonly EmitBaseline Baseline;

        protected DefinitionMap(IEnumerable<SemanticEdit> edits, EmitBaseline baseline)
        {
            Debug.Assert(edits != null);

            mappedMethods = GetMappedMethods(edits);

            _methodInstrumentations = edits
                .Where(edit => !edit.Instrumentation.IsEmpty)
                .ToImmutableDictionary(edit => (IMethodSymbolInternal)GetISymbolInternalOrNull(edit.NewSymbol!)!, edit => edit.Instrumentation);

            Baseline = baseline;
        }

        private IReadOnlyDictionary<IMethodSymbolInternal, MappedMethod> GetMappedMethods(IEnumerable<SemanticEdit> edits)
        {
            var mappedMethods = new Dictionary<IMethodSymbolInternal, MappedMethod>();
            foreach (var edit in edits)
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

                // TODO (bug https://github.com/dotnet/roslyn/issues/2504)
                // Note that in some cases an Insert might also need to map syntax. For example, a new constructor added 
                // to a class that has field/property initializers with lambdas. These lambdas get "copied" into the constructor
                // (assuming it doesn't have "this" constructor initializer) and thus their generated names need to be preserved. 

                if (edit.Kind == SemanticEditKind.Update && edit.SyntaxMap != null)
                {
                    RoslynDebug.AssertNotNull(edit.NewSymbol);
                    RoslynDebug.AssertNotNull(edit.OldSymbol);

                    if (GetISymbolInternalOrNull(edit.NewSymbol) is IMethodSymbolInternal newMethod &&
                        GetISymbolInternalOrNull(edit.OldSymbol) is IMethodSymbolInternal oldMethod)
                    {
                        mappedMethods.Add(newMethod, new MappedMethod(oldMethod, edit.SyntaxMap));
                    }
                }
            }

            return mappedMethods;
        }

        protected abstract ISymbolInternal? GetISymbolInternalOrNull(ISymbol symbol);

        internal Cci.IDefinition? MapDefinition(Cci.IDefinition definition)
        {
            return SourceToPreviousSymbolMatcher.MapDefinition(definition) ??
                   (SourceToMetadataSymbolMatcher != SourceToPreviousSymbolMatcher ? SourceToMetadataSymbolMatcher.MapDefinition(definition) : null);
        }

        internal Cci.INamespace? MapNamespace(Cci.INamespace @namespace)
        {
            return SourceToPreviousSymbolMatcher.MapNamespace(@namespace) ??
                   (SourceToMetadataSymbolMatcher != SourceToPreviousSymbolMatcher ? SourceToMetadataSymbolMatcher.MapNamespace(@namespace) : null);
        }

        internal bool DefinitionExists(Cci.IDefinition definition)
            => MapDefinition(definition) is object;

        internal bool NamespaceExists(Cci.INamespace @namespace)
            => MapNamespace(@namespace) is object;

        internal EntityHandle GetInitialMetadataHandle(Cci.IDefinition def)
            => MetadataTokens.EntityHandle(SourceToMetadataSymbolMatcher.MapDefinition(def)?.GetInternalSymbol()?.MetadataToken ?? 0);

        public abstract SymbolMatcher SourceToMetadataSymbolMatcher { get; }
        public abstract SymbolMatcher SourceToPreviousSymbolMatcher { get; }
        public abstract SymbolMatcher PreviousSourceToMetadataSymbolMatcher { get; }

        internal abstract CommonMessageProvider MessageProvider { get; }

        /// <summary>
        /// Gets a <see cref="MethodDefinitionHandle"/> for a given <paramref name="method"/>,
        /// if it is defined in the initial metadata or has been added since.
        /// </summary>
        public bool TryGetMethodHandle(IMethodSymbolInternal method, out MethodDefinitionHandle handle)
        {
            var methodDef = (Cci.IMethodDefinition)method.GetCciAdapter();

            if (GetInitialMetadataHandle(methodDef) is { IsNil: false } entityHandle)
            {
                handle = (MethodDefinitionHandle)entityHandle;
                return true;
            }

            var mappedDef = (Cci.IMethodDefinition?)SourceToPreviousSymbolMatcher.MapDefinition(methodDef);
            if (mappedDef != null && Baseline.MethodsAdded.TryGetValue(mappedDef, out int methodIndex))
            {
                handle = MetadataTokens.MethodDefinitionHandle(methodIndex);
                return true;
            }

            handle = default;
            return false;
        }

        public MethodDefinitionHandle GetPreviousMethodHandle(IMethodSymbolInternal oldMethod)
            => GetPreviousMethodHandle(oldMethod, out _);

        /// <summary>
        /// Returns method handle of a method symbol from the immediately preceding generation.
        /// </summary>
        /// <remarks>
        /// The method may have been defined in any preceding generation but <paramref name="oldMethod"/> symbol must be mapped to
        /// the immediately preceding one.
        /// </remarks>
        public MethodDefinitionHandle GetPreviousMethodHandle(IMethodSymbolInternal oldMethod, out IMethodSymbolInternal? peMethod)
        {
            var oldMethodDef = (Cci.IMethodDefinition)oldMethod.GetCciAdapter();

            if (Baseline.MethodsAdded.TryGetValue(oldMethodDef, out var methodRowId))
            {
                peMethod = null;
                return MetadataTokens.MethodDefinitionHandle(methodRowId);
            }
            else
            {
                peMethod = (IMethodSymbolInternal?)PreviousSourceToMetadataSymbolMatcher.MapDefinition(oldMethodDef)?.GetInternalSymbol();
                Debug.Assert(peMethod != null);
                Debug.Assert(peMethod.MetadataName == oldMethod.MetadataName);

                return (MethodDefinitionHandle)MetadataTokens.EntityHandle(peMethod.MetadataToken);
            }
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
            ITypeSymbolInternal stateMachineType,
            ImmutableArray<LocalSlotDebugInfo> localSlotDebugInfo,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap,
            out int awaiterSlotCount);

        protected abstract ImmutableArray<EncLocalInfo> GetLocalSlotMapFromMetadata(StandaloneSignatureHandle handle, EditAndContinueMethodDebugInformation debugInfo);
        protected abstract ITypeSymbolInternal? TryGetStateMachineType(MethodDefinitionHandle methodHandle);
        protected abstract IMethodSymbolInternal GetMethodSymbol(MethodDefinitionHandle methodHandle);

        internal VariableSlotAllocator? TryCreateVariableSlotAllocator(Compilation compilation, IMethodSymbolInternal method, IMethodSymbolInternal topLevelMethod, DiagnosticBag diagnostics)
        {
            // Top-level methods are always included in the semantic edit list. Lambda methods are not.
            if (!mappedMethods.TryGetValue(topLevelMethod, out var mappedMethod))
            {
                return null;
            }

            // TODO (bug https://github.com/dotnet/roslyn/issues/2504):
            // Handle cases when the previous method doesn't exist.

            if (!TryGetMethodHandle(method, out var methodHandle))
            {
                // Unrecognized method. Must have been added in the current compilation.
                return null;
            }

            ImmutableArray<EncLocalInfo> previousLocals;
            IReadOnlyDictionary<EncHoistedLocalInfo, int>? hoistedLocalMap = null;
            IReadOnlyDictionary<Cci.ITypeReference, int>? awaiterMap = null;
            IReadOnlyDictionary<int, KeyValuePair<DebugId, int>>? lambdaMap = null;
            IReadOnlyDictionary<int, DebugId>? closureMap = null;
            IReadOnlyDictionary<(int syntaxOffset, AwaitDebugId debugId), StateMachineState>? stateMachineStateMap = null;
            StateMachineState? firstUnusedIncreasingStateMachineState = null;
            StateMachineState? firstUnusedDecreasingStateMachineState = null;

            int hoistedLocalSlotCount = 0;
            int awaiterSlotCount = 0;
            string? stateMachineTypeName = null;
            SymbolMatcher symbolMap;

            int methodIndex = MetadataTokens.GetRowNumber(methodHandle);
            DebugId methodId;

            // Check if method has changed previously. If so, we already have a map.
            if (Baseline.AddedOrChangedMethods.TryGetValue(methodIndex, out var addedOrChangedMethod))
            {
                methodId = addedOrChangedMethod.MethodId;

                MakeLambdaAndClosureMaps(addedOrChangedMethod.LambdaDebugInfo, addedOrChangedMethod.ClosureDebugInfo, out lambdaMap, out closureMap);
                MakeStateMachineStateMap(addedOrChangedMethod.StateMachineStates.States, out stateMachineStateMap);

                firstUnusedIncreasingStateMachineState = addedOrChangedMethod.StateMachineStates.FirstUnusedIncreasingStateMachineState;
                firstUnusedDecreasingStateMachineState = addedOrChangedMethod.StateMachineStates.FirstUnusedDecreasingStateMachineState;

                if (addedOrChangedMethod.StateMachineTypeName != null)
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
                    // We use the EnC method debug information for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    stateMachineTypeName = addedOrChangedMethod.StateMachineTypeName;
                }
                else
                {
                    previousLocals = addedOrChangedMethod.Locals;
                }

                // All types that AddedOrChangedMethodInfo refers to have been mapped to the previous generation.
                // Therefore we don't need to fall back to metadata if we don't find the type reference, like we do in DefinitionMap.MapReference.
                symbolMap = SourceToPreviousSymbolMatcher;
            }
            else
            {
                // Method has not changed since initial generation. Generate a map
                // using the local names provided with the initial metadata.
                EditAndContinueMethodDebugInformation debugInfo;
                StandaloneSignatureHandle localSignature;
                try
                {
                    debugInfo = Baseline.DebugInformationProvider(methodHandle);
                    localSignature = Baseline.LocalSignatureProvider(methodHandle);
                }
                catch (Exception e) when (e is InvalidDataException or IOException)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(
                        MessageProvider.ERR_InvalidDebugInfo,
                        method.Locations.First(),
                        method,
                        MetadataTokens.GetToken(methodHandle),
                        method.ContainingAssembly
                    ));

                    return null;
                }

                methodId = new DebugId(debugInfo.MethodOrdinal, 0);

                if (!debugInfo.Lambdas.IsDefaultOrEmpty)
                {
                    MakeLambdaAndClosureMaps(debugInfo.Lambdas, debugInfo.Closures, out lambdaMap, out closureMap);
                }

                MakeStateMachineStateMap(debugInfo.StateMachineStates, out stateMachineStateMap);

                if (!debugInfo.StateMachineStates.IsDefaultOrEmpty)
                {
                    firstUnusedIncreasingStateMachineState = debugInfo.StateMachineStates.Max(s => s.StateNumber) + 1;
                    firstUnusedDecreasingStateMachineState = debugInfo.StateMachineStates.Min(s => s.StateNumber) - 1;
                }

                ITypeSymbolInternal? stateMachineType = TryGetStateMachineType(methodHandle);
                if (stateMachineType != null)
                {
                    // Method is async/iterator kickoff method.

                    // Use local slots stored in CDI (encLocalSlotMap) to calculate map of local variables hoisted to fields of the state machine.
                    var localSlotDebugInfo = debugInfo.LocalSlots.NullToEmpty();
                    GetStateMachineFieldMapFromMetadata(stateMachineType, localSlotDebugInfo, out hoistedLocalMap, out awaiterMap, out awaiterSlotCount);
                    hoistedLocalSlotCount = localSlotDebugInfo.Length;

                    // Kickoff method has no interesting locals on its own. 
                    // We use the EnC method debug information for hoisted locals.
                    previousLocals = ImmutableArray<EncLocalInfo>.Empty;

                    stateMachineTypeName = stateMachineType.Name;
                }
                else
                {
                    // If the current method is async/iterator then either the previous method wasn't declared as async/iterator and it's updated to be one,
                    // or it was but is not marked by the corresponding state machine attribute because it was missing in the compilation. 
                    // In the later case we need to report an error since we don't known how to map to the previous state machine.

                    // The IDE already checked that the attribute type is present in the base compilation, but didn't validate that it is well-formed.
                    // We don't have the base compilation to directly query for the attribute, only the source compilation. 
                    // But since constructor signatures can't be updated during EnC we can just check the current compilation.

                    if (method.IsAsync)
                    {
                        if (compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor) == null)
                        {
                            ReportMissingStateMachineAttribute(diagnostics, method, AttributeDescription.AsyncStateMachineAttribute.FullName);
                            return null;
                        }
                    }
                    else if (method.IsIterator)
                    {
                        if (compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor) == null)
                        {
                            ReportMissingStateMachineAttribute(diagnostics, method, AttributeDescription.IteratorStateMachineAttribute.FullName);
                            return null;
                        }
                    }

                    // Calculate local slot mapping for the current method (might be the MoveNext method of a state machine).

                    try
                    {
                        previousLocals = localSignature.IsNil ? ImmutableArray<EncLocalInfo>.Empty :
                            GetLocalSlotMapFromMetadata(localSignature, debugInfo);
                    }
                    catch (Exception e) when (e is UnsupportedSignatureContent || e is BadImageFormatException || e is IOException)
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(
                            MessageProvider.ERR_InvalidDebugInfo,
                            method.Locations.First(),
                            method,
                            MetadataTokens.GetToken(localSignature),
                            method.ContainingAssembly
                        ));

                        return null;
                    }
                }

                symbolMap = SourceToMetadataSymbolMatcher;
            }

            return new EncVariableSlotAllocator(
                symbolMap,
                mappedMethod.SyntaxMap,
                mappedMethod.PreviousMethod,
                methodId,
                previousLocals,
                lambdaMap,
                closureMap,
                stateMachineTypeName,
                hoistedLocalSlotCount,
                hoistedLocalMap,
                awaiterSlotCount,
                awaiterMap,
                stateMachineStateMap,
                firstUnusedIncreasingStateMachineState,
                firstUnusedDecreasingStateMachineState,
                GetLambdaSyntaxFacts());
        }

        internal MethodInstrumentation GetMethodBodyInstrumentations(IMethodSymbolInternal method)
            => _methodInstrumentations.TryGetValue(method, out var instrumentation) ? instrumentation : MethodInstrumentation.Empty;

        protected abstract LambdaSyntaxFacts GetLambdaSyntaxFacts();

        private void ReportMissingStateMachineAttribute(DiagnosticBag diagnostics, IMethodSymbolInternal method, string stateMachineAttributeFullName)
        {
            diagnostics.Add(MessageProvider.CreateDiagnostic(
                MessageProvider.ERR_EncUpdateFailedMissingAttribute,
                method.Locations.First(),
                MessageProvider.GetErrorDisplayString(method.GetISymbol()),
                stateMachineAttributeFullName));
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
                lambdas[lambdaInfo.SyntaxOffset] = KeyValuePairUtil.Create(lambdaInfo.LambdaId, lambdaInfo.ClosureOrdinal);
            }

            for (int i = 0; i < closureDebugInfo.Length; i++)
            {
                var closureInfo = closureDebugInfo[i];
                closures[closureInfo.SyntaxOffset] = closureInfo.ClosureId;
            }

            lambdaMap = lambdas;
            closureMap = closures;
        }

        private static void MakeStateMachineStateMap(
            ImmutableArray<StateMachineStateDebugInfo> debugInfos,
            out IReadOnlyDictionary<(int syntaxOffset, AwaitDebugId debugId), StateMachineState>? map)
        {
            map = debugInfos.IsDefault ?
                null :
                debugInfos.ToDictionary(entry => (entry.SyntaxOffset, entry.AwaitId), entry => entry.StateNumber);
        }

        private static void GetStateMachineFieldMapFromPreviousCompilation(
            ImmutableArray<EncHoistedLocalInfo> hoistedLocalSlots,
            ImmutableArray<Cci.ITypeReference?> hoistedAwaiters,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap)
        {
            var hoistedLocals = new Dictionary<EncHoistedLocalInfo, int>();
            var awaiters = new Dictionary<Cci.ITypeReference, int>(Cci.SymbolEquivalentEqualityComparer.Instance);

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

        protected abstract bool TryParseDisplayClassOrLambdaName(
            string name,
            out int suffixIndex,
            out char idSeparator,
            out bool isDisplayClass,
            out bool hasDebugIds);

        private IEnumerable<IMethodSymbolInternal> GetSynthesizedClosureMethods(
            ImmutableArray<ISymbolInternal> synthesizedMembers,
            IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? synthesizedMemberMap,
            DebugId methodId,
            HashSet<DebugId> lambdaIds)
        {
            return recurse(synthesizedMembers, inSpecificDisplayClass: false);

            IEnumerable<IMethodSymbolInternal> recurse(ImmutableArray<ISymbolInternal> synthesizedMembers, bool inSpecificDisplayClass)
            {
                foreach (var synthesizedMember in synthesizedMembers)
                {
                    var memberName = synthesizedMember.Name;

                    if (TryParseDisplayClassOrLambdaName(memberName, out int suffixIndex, out char idSeparator, out bool isDisplayClass, out bool hasDebugIds))
                    {
                        // If we are in a display class that is specific to a method the original method id is already incorporated to the name of the display class,
                        // so members do not have it in their name and only have the entity id.

                        var suffixSpan = memberName.AsSpan(suffixIndex);

                        DebugId parsedMethodId = default;
                        DebugId parsedEntityId = default;
                        if (hasDebugIds)
                        {
                            if (!CommonGeneratedNames.TryParseDebugIds(suffixSpan, idSeparator, isMethodIdOptional: inSpecificDisplayClass, out parsedMethodId, out parsedEntityId))
                            {
                                // name is not well-formed
                                continue;
                            }

                            if (!inSpecificDisplayClass && parsedMethodId != methodId)
                            {
                                // synthesized member belongs to a different method
                                continue;
                            }
                        }

                        if (isDisplayClass)
                        {
                            // display classes are not nested:
                            Debug.Assert(!inSpecificDisplayClass);

                            var displayClass = (INamedTypeSymbolInternal)synthesizedMember;
                            var displayClassMembers = (synthesizedMemberMap != null) ? synthesizedMemberMap[displayClass] : displayClass.GetMembers();

                            foreach (var displayClassMember in recurse(displayClassMembers, inSpecificDisplayClass: hasDebugIds))
                            {
                                yield return displayClassMember;
                            }
                        }
                        else
                        {
                            Debug.Assert(hasDebugIds);

                            if (lambdaIds.Contains(parsedEntityId))
                            {
                                yield return (IMethodSymbolInternal)synthesizedMember;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates method symbols synthesized for the body of a given <paramref name="oldMethod"/> in the previous generation that are not synthesized for the current method body (if any).
        /// </summary>
        /// <param name="oldMethod">Method from the previous generation.</param>
        /// <param name="currentLambdas">Lambdas generated to the current version of the method. This includes both lambdas mapped to previous ones and newly introduced lambdas.</param>
        public IEnumerable<IMethodSymbolInternal> GetDeletedSynthesizedMethods(IMethodSymbolInternal oldMethod, ImmutableArray<LambdaDebugInfo> currentLambdas)
        {
            var methodHandle = GetPreviousMethodHandle(oldMethod, out var peMethod);
            var methodRowId = MetadataTokens.GetRowNumber(methodHandle);

            if (Baseline.AddedOrChangedMethods.TryGetValue(methodRowId, out var addedOrChangedMethod))
            {
                // If a method has been added or updated then all synthesized members it produced are stored on the baseline.
                // This includes all lambdas regardless of whether they were mapped to previous generation or not.
                if (!addedOrChangedMethod.LambdaDebugInfo.IsDefaultOrEmpty &&
                    Baseline.SynthesizedMembers.TryGetValue(oldMethod.ContainingType, out var synthesizedSiblingSymbols))
                {
                    return getDeletedSynthesizedClosureMethods(synthesizedSiblingSymbols, Baseline.SynthesizedMembers, addedOrChangedMethod.MethodId, addedOrChangedMethod.LambdaDebugInfo);
                }

                return SpecializedCollections.EmptyEnumerable<IMethodSymbolInternal>();
            }

            Debug.Assert(peMethod != null);

            EditAndContinueMethodDebugInformation provider;
            try
            {
                provider = Baseline.DebugInformationProvider(MetadataTokens.MethodDefinitionHandle(methodRowId));
            }
            catch (Exception e) when (e is InvalidDataException or IOException)
            {
                return SpecializedCollections.EmptyEnumerable<IMethodSymbolInternal>();
            }

            if (provider.Lambdas.IsDefaultOrEmpty)
            {
                return SpecializedCollections.EmptyEnumerable<IMethodSymbolInternal>();
            }

            var debugId = new DebugId(provider.MethodOrdinal, generation: 0);
            return getDeletedSynthesizedClosureMethods(peMethod.ContainingType.GetMembers(), synthesizedMemberMap: null, debugId, provider.Lambdas);

            IEnumerable<IMethodSymbolInternal> getDeletedSynthesizedClosureMethods(
                ImmutableArray<ISymbolInternal> synthesizedSiblings,
                IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>? synthesizedMemberMap,
                DebugId methodId,
                ImmutableArray<LambdaDebugInfo> previousLambdas)
            {
                var lambdaIdSet = PooledHashSet<DebugId>.GetInstance();

                foreach (var info in previousLambdas)
                {
                    lambdaIdSet.Add(info.LambdaId);
                }

                foreach (var info in currentLambdas)
                {
                    lambdaIdSet.Remove(info.LambdaId);
                }

                foreach (var method in GetSynthesizedClosureMethods(synthesizedSiblings, synthesizedMemberMap, methodId, lambdaIdSet))
                {
                    yield return method;
                }

                lambdaIdSet.Free();
            }
        }
    }
}
