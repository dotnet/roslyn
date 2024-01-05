// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    internal sealed class CSharpDefinitionMap(
        IEnumerable<SemanticEdit> edits,
        MetadataDecoder metadataDecoder,
        CSharpSymbolMatcher previousSourceToMetadata,
        CSharpSymbolMatcher sourceToMetadata,
        CSharpSymbolMatcher? previousSourceToCurrentSource,
        EmitBaseline baseline) : DefinitionMap(edits, baseline)
    {
        private readonly CSharpSymbolMatcher _sourceToPrevious = previousSourceToCurrentSource ?? sourceToMetadata;

        public override SymbolMatcher SourceToMetadataSymbolMatcher => sourceToMetadata;
        public override SymbolMatcher SourceToPreviousSymbolMatcher => _sourceToPrevious;
        public override SymbolMatcher PreviousSourceToMetadataSymbolMatcher => previousSourceToMetadata;

        protected override ISymbolInternal? GetISymbolInternalOrNull(ISymbol symbol)
        {
            return (symbol as Symbols.PublicModel.Symbol)?.UnderlyingSymbol;
        }

        internal override CommonMessageProvider MessageProvider
            => CSharp.MessageProvider.Instance;

        protected override LambdaSyntaxFacts GetLambdaSyntaxFacts()
            => CSharpLambdaSyntaxFacts.Instance;

        internal bool TryGetAnonymousTypeValue(AnonymousTypeManager.AnonymousTypeOrDelegateTemplateSymbol template, out AnonymousTypeValue typeValue)
            => _sourceToPrevious.TryGetAnonymousTypeValue(template, out typeValue);

        protected override void GetStateMachineFieldMapFromMetadata(
            ITypeSymbolInternal stateMachineType,
            ImmutableArray<LocalSlotDebugInfo> localSlotDebugInfo,
            out IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalMap,
            out IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMap,
            out int awaiterSlotCount)
        {
            // we are working with PE symbols
            Debug.Assert(stateMachineType.ContainingAssembly is PEAssemblySymbol);

            var hoistedLocals = new Dictionary<EncHoistedLocalInfo, int>();
            var awaiters = new Dictionary<Cci.ITypeReference, int>(Cci.SymbolEquivalentEqualityComparer.Instance);
            int maxAwaiterSlotIndex = -1;

            foreach (var member in ((TypeSymbol)stateMachineType).GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                {
                    string name = member.Name;
                    int slotIndex;

                    switch (GeneratedNameParser.GetKind(name))
                    {
                        case GeneratedNameKind.AwaiterField:
                            if (GeneratedNameParser.TryParseSlotIndex(name, out slotIndex))
                            {
                                var field = (FieldSymbol)member;

                                // correct metadata won't contain duplicates, but malformed might, ignore the duplicate:
                                awaiters[(Cci.ITypeReference)field.Type.GetCciAdapter()] = slotIndex;

                                if (slotIndex > maxAwaiterSlotIndex)
                                {
                                    maxAwaiterSlotIndex = slotIndex;
                                }
                            }

                            break;

                        case GeneratedNameKind.HoistedLocalField:
                        case GeneratedNameKind.HoistedSynthesizedLocalField:
                        case GeneratedNameKind.DisplayClassLocalOrField:
                            if (GeneratedNameParser.TryParseSlotIndex(name, out slotIndex))
                            {
                                var field = (FieldSymbol)member;
                                if (slotIndex >= localSlotDebugInfo.Length)
                                {
                                    // invalid or missing metadata
                                    continue;
                                }

                                var key = new EncHoistedLocalInfo(localSlotDebugInfo[slotIndex], (Cci.ITypeReference)field.Type.GetCciAdapter());

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

            var localInfos = metadataDecoder.GetLocalsOrThrow(handle);
            var result = CreateLocalSlotMap(debugInfo, localInfos);
            Debug.Assert(result.Length == localInfos.Length);
            return result;
        }

        protected override ITypeSymbolInternal? TryGetStateMachineType(MethodDefinitionHandle methodHandle)
            => metadataDecoder.Module.HasStateMachineAttribute(methodHandle, out var typeName) ? metadataDecoder.GetTypeSymbolForSerializedType(typeName) : null;

        protected override IMethodSymbolInternal GetMethodSymbol(MethodDefinitionHandle methodHandle)
            => (IMethodSymbolInternal)metadataDecoder.GetSymbolForILToken(methodHandle);

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
                            var local = new EncLocalInfo(slot, (Cci.ITypeReference)metadata.Type.GetCciAdapter(), metadata.Constraints, metadata.SignatureOpt);
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

        protected override bool TryParseDisplayClassOrLambdaName(
            string name,
            out int suffixIndex,
            out char idSeparator,
            out bool isDisplayClass,
            out bool isDisplayClassParentField,
            out bool hasDebugIds)
        {
            suffixIndex = 0;
            isDisplayClass = false;
            isDisplayClassParentField = false;
            hasDebugIds = false;
            idSeparator = GeneratedNameConstants.IdSeparator;

            if (!GeneratedNameParser.TryParseGeneratedName(name, out var generatedKind, out _, out var closeBracketOffset))
            {
                return false;
            }

            if (generatedKind is not (GeneratedNameKind.LambdaDisplayClass or GeneratedNameKind.LambdaMethod or GeneratedNameKind.LocalFunction or GeneratedNameKind.DisplayClassLocalOrField))
            {
                return false;
            }

            // close bracket is followed by kind character:
            Debug.Assert(name.Length >= closeBracketOffset + 1);

            isDisplayClass = generatedKind == GeneratedNameKind.LambdaDisplayClass;
            isDisplayClassParentField = generatedKind == GeneratedNameKind.DisplayClassLocalOrField;

            suffixIndex = closeBracketOffset + 2;
            hasDebugIds = !isDisplayClassParentField && name.AsSpan(suffixIndex).StartsWith(GeneratedNameConstants.SuffixSeparator.AsSpan(), StringComparison.Ordinal);

            if (hasDebugIds)
            {
                suffixIndex += GeneratedNameConstants.SuffixSeparator.Length;
            }

            return true;
        }
    }
}
