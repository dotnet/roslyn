// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

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
        private readonly SymbolMatcher mapToMetadata;
        private readonly SymbolMatcher mapToPrevious;
        
        public CSharpDefinitionMap(
            PEModule module,
            IEnumerable<SemanticEdit> edits,
            MetadataDecoder metadataDecoder,
            SymbolMatcher mapToMetadata,
            SymbolMatcher mapToPrevious)
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

        internal override bool TryGetPreviousLocals(
            EmitBaseline baseline,
            IMethodSymbol method,
            out ImmutableArray<EncLocalInfo> previousLocals,
            out GetPreviousLocalSlot getPreviousLocalSlot)
        {
            previousLocals = default(ImmutableArray<EncLocalInfo>);
            getPreviousLocalSlot = NoPreviousLocalSlot;

            MethodHandle handle;
            if (!this.TryGetMethodHandle(baseline, (Cci.IMethodDefinition)method, out handle))
            {
                // Unrecognized method. Must have been added in the current compilation.
                return false;
            }

            MethodDefinitionEntry methodEntry;
            if (!this.methodMap.TryGetValue(method, out methodEntry))
            {
                // Not part of changeset. No need to preserve locals.
                return false;
            }

            if (!methodEntry.PreserveLocalVariables)
            {
                // Not necessary to preserve locals.
                return false;
            }

            var previousMethod = methodEntry.PreviousMethod;
            var methodIndex = (uint)MetadataTokens.GetRowNumber(handle);
            SymbolMatcher map;

            // Check if method has changed previously. If so, we already have a map.
            if (baseline.LocalsForMethodsAddedOrChanged.TryGetValue(methodIndex, out previousLocals))
            {
                map = this.mapToPrevious;
            }
            else
            {
                // Method has not changed since initial generation. Generate a map
                // using the local names provided with the initial metadata.
                var localNames = baseline.LocalNames(methodIndex);
                Debug.Assert(!localNames.IsDefault);

                var localInfo = default(ImmutableArray<MetadataDecoder.LocalInfo>);
                try
                {
                    Debug.Assert(this.module.HasIL);
                    var methodBody = this.module.GetMethodBodyOrThrow(handle);

                    if (!methodBody.LocalSignature.IsNil)
                    {
                        var signature = this.module.MetadataReader.GetLocalSignature(methodBody.LocalSignature);
                        localInfo = this.metadataDecoder.DecodeLocalSignatureOrThrow(signature);
                    }
                    else
                    {
                        localInfo = ImmutableArray<MetadataDecoder.LocalInfo>.Empty;
                    }
                }
                catch (UnsupportedSignatureContent)
                {
                }
                catch (BadImageFormatException)
                {
                }

                if (localInfo.IsDefault)
                {
                    // TODO: Report error that metadata is not supported.
                    return false;
                }
                else
                {
                    // The signature may have more locals than names if trailing locals are unnamed.
                    // (Locals in the middle of the signature may be unnamed too but since localNames
                    // is indexed by slot, unnamed locals before the last named local will be represented
                    // as null values in the array.)
                    Debug.Assert(localInfo.Length >= localNames.Length);
                    previousLocals = GetLocalSlots(previousMethod, localNames, localInfo);
                    Debug.Assert(previousLocals.Length == localInfo.Length);
                }

                map = this.mapToMetadata;
            }

            // Find declarators in previous method syntax.
            // The locals are indices into this list.
            var previousDeclarators = LocalVariableDeclaratorsCollector.GetDeclarators(previousMethod);

            // Create a map from declarator to declarator offset.
            var previousDeclaratorToOffset = new Dictionary<SyntaxNode, int>();
            for (int offset = 0; offset < previousDeclarators.Length; offset++)
            {
                previousDeclaratorToOffset.Add(previousDeclarators[offset], offset);
            }

            // Create a map from local info to slot.
            var previousLocalInfoToSlot = new Dictionary<EncLocalInfo, int>();
            for (int slot = 0; slot < previousLocals.Length; slot++)
            {
                var localInfo = previousLocals[slot];
                Debug.Assert(!localInfo.IsDefault);
                if (localInfo.IsInvalid)
                {
                    // Unrecognized or deleted local.
                    continue;
                }
                previousLocalInfoToSlot.Add(localInfo, slot);
            }

            var syntaxMap = methodEntry.SyntaxMap;
            if (syntaxMap == null)
            {
                // If there was no syntax map, the syntax structure has not changed,
                // so we can map from current to previous syntax by declarator index.
                Debug.Assert(methodEntry.PreserveLocalVariables);
                // Create a map from declarator to declarator index.
                var currentDeclarators = LocalVariableDeclaratorsCollector.GetDeclarators(method);
                var currentDeclaratorToIndex = CreateDeclaratorToIndexMap(currentDeclarators);
                syntaxMap = currentSyntax =>
                {
                    var currentIndex = currentDeclaratorToIndex[(CSharpSyntaxNode)currentSyntax];
                    return previousDeclarators[currentIndex];
                };
            }

            getPreviousLocalSlot = (object identity, Cci.ITypeReference typeRef, LocalSlotConstraints constraints) =>
            {
                var local = (LocalSymbol)identity;
                var syntaxRefs = local.DeclaringSyntaxReferences;
                Debug.Assert(!syntaxRefs.IsDefault);

                if (!syntaxRefs.IsDefaultOrEmpty)
                {
                    var currentSyntax = syntaxRefs[0].GetSyntax();
                    var previousSyntax = (CSharpSyntaxNode)syntaxMap(currentSyntax);
                    if (previousSyntax != null)
                    {
                        int offset;
                        if (previousDeclaratorToOffset.TryGetValue(previousSyntax, out offset))
                        {
                            var previousType = map.MapReference(typeRef);
                            if (previousType != null)
                            {
                                var localKey = new EncLocalInfo(offset, previousType, constraints, (int)local.TempKind);
                                int slot;
                                // Should report a warning if the type of the local has changed
                                // and the previous value will be dropped. (Bug #781309.)
                                if (previousLocalInfoToSlot.TryGetValue(localKey, out slot))
                                {
                                    return slot;
                                }
                            }
                        }
                    }
                }

                return -1;
            };

            return true;
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

        private static IReadOnlyDictionary<SyntaxNode, int> CreateDeclaratorToIndexMap(ImmutableArray<SyntaxNode> declarators)
        {
            var declaratorToIndex = new Dictionary<SyntaxNode, int>();
            for (int i = 0; i < declarators.Length; i++)
            {
                declaratorToIndex.Add(declarators[i], i);
            }
            return declaratorToIndex;
        }

        internal override ImmutableArray<EncLocalInfo> GetLocalInfo(
            Cci.IMethodDefinition methodDef,
            ImmutableArray<LocalDefinition> localDefs)
        {
            if (localDefs.IsEmpty)
            {
                return ImmutableArray<EncLocalInfo>.Empty;
            }

            // Find declarators in current method syntax.
            var declarators = LocalVariableDeclaratorsCollector.GetDeclarators((MethodSymbol)methodDef);

            // Create a map from declarator to declarator index.
            var declaratorToIndex = CreateDeclaratorToIndexMap(declarators);

            return localDefs.SelectAsArray(localDef => GetLocalInfo(declaratorToIndex, localDef));
        }

        private static EncLocalInfo GetLocalInfo(
            IReadOnlyDictionary<SyntaxNode, int> declaratorToIndex,
            LocalDefinition localDef)
        {
            // Local symbol will be null for short-lived temporaries.
            var local = (LocalSymbol)localDef.Identity;
            if ((object)local != null)
            {
                var syntaxRefs = local.DeclaringSyntaxReferences;
                Debug.Assert(!syntaxRefs.IsDefault);

                if (!syntaxRefs.IsDefaultOrEmpty)
                {
                    var syntax = syntaxRefs[0].GetSyntax();
                    var offset = declaratorToIndex[syntax];
                    return new EncLocalInfo(offset, localDef.Type, localDef.Constraints, (int)local.TempKind);
                }
            }

            return new EncLocalInfo(localDef.Type, localDef.Constraints);
        }

        /// <summary>
        /// Match local declarations to names to generate a map from
        /// declaration to local slot. The names are indexed by slot and the
        /// assumption is that declarations are in the same order as slots.
        /// </summary>
        private static ImmutableArray<EncLocalInfo> GetLocalSlots(
            IMethodSymbol method,
            ImmutableArray<string> localNames,
            ImmutableArray<MetadataDecoder.LocalInfo> localInfo)
        {
            var syntaxRefs = method.DeclaringSyntaxReferences;

            // No syntax refs for synthesized methods.
            if (syntaxRefs.Length == 0)
            {
                return ImmutableArray<EncLocalInfo>.Empty;
            }

            var syntax = syntaxRefs[0].GetSyntax();
            var map = LocalSlotMapBuilder.CreateMap(syntax, localNames, localInfo);
            var locals = new EncLocalInfo[localInfo.Length];
            foreach (var pair in map)
            {
                locals[pair.Value] = pair.Key;
            }

            // Populate any remaining locals that were not matched to source.
            for (int i = 0; i < locals.Length; i++)
            {
                if (locals[i].IsDefault)
                {
                    var info = localInfo[i];
                    var constraints = GetConstraints(info);
                    locals[i] = new EncLocalInfo((Cci.ITypeReference)info.Type, constraints);
                }
            }

            return ImmutableArray.Create(locals);
        }

        private static LocalSlotConstraints GetConstraints(MetadataDecoder.LocalInfo info)
        {
            return (info.IsPinned ? LocalSlotConstraints.Pinned : LocalSlotConstraints.None) |
                (info.IsByRef ? LocalSlotConstraints.ByRef : LocalSlotConstraints.None);
        }
    }
}
