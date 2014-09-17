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
                var localNames = baseline.LocalNames(handle);
                Debug.Assert(!localNames.IsDefault);

                ImmutableArray<MetadataDecoder.LocalInfo> localInfo;
                if (!metadataDecoder.TryGetLocals(handle, out localInfo))
                {
                    // TODO: Report error that metadata is not supported.
                    return null;
                }

                // The signature may have more locals than names if trailing locals are unnamed.
                // (Locals in the middle of the signature may be unnamed too but since localNames
                // is indexed by slot, unnamed locals before the last named local will be represented
                // as null values in the array.)
                Debug.Assert(localInfo.Length >= localNames.Length);

                previousLocals = GetLocalSlots(methodEntry.PreviousMethod, localNames, localInfo);
                Debug.Assert(previousLocals.Length == localInfo.Length);
                symbolMap = this.mapToMetadata;
            }

            // Find declarators in previous method syntax.
            // The locals are indices into this list.
            var previousDeclarators = LocalVariableDeclaratorsCollector.GetDeclarators(methodEntry.PreviousMethod);

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
                    var currentIndex = currentDeclaratorToIndex[currentSyntax];
                    return previousDeclarators[currentIndex];
                };
            }

            return new EncVariableSlotAllocator(symbolMap, syntaxMap, previousDeclarators, previousLocals);
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
            ImmutableArray<Cci.ILocalDefinition> localDefs,
            ImmutableArray<byte[]> signatures)
        {
            if (localDefs.IsEmpty)
            {
                return ImmutableArray<EncLocalInfo>.Empty;
            }

            // Find declarators in current method syntax.
            var declarators = LocalVariableDeclaratorsCollector.GetDeclarators((MethodSymbol)methodDef);

            // Create a map from declarator to declarator index.
            var declaratorToIndex = CreateDeclaratorToIndexMap(declarators);

            return localDefs.SelectAsArray((localDef, i, arg) => GetLocalInfo(declaratorToIndex, localDef, signatures[i]), (object)null);
        }

        private static EncLocalInfo GetLocalInfo(
            IReadOnlyDictionary<SyntaxNode, int> declaratorToIndex,
            Cci.ILocalDefinition localDef,
            byte[] signature)
        {
            var def = localDef as LocalDefinition;
            if (def != null)
            {
                // Local symbol will be null for short-lived temporaries.
                var local = def.SymbolOpt;
                if ((object)local != null)
                {
                    var syntaxRefs = local.DeclaringSyntaxReferences;
                    Debug.Assert(!syntaxRefs.IsDefault);

                    if (!syntaxRefs.IsDefaultOrEmpty)
                    {
                        var syntax = syntaxRefs[0].GetSyntax();
                        var offset = declaratorToIndex[syntax];
                        return new EncLocalInfo(offset, localDef.Type, def.Constraints, def.SynthesizedLocalKind, signature);
                    }
                }
            }

            return new EncLocalInfo(signature);
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
                    locals[i] = new EncLocalInfo(localInfo[i].SignatureOpt);
                }
            }

            return ImmutableArray.Create(locals);
        }
    }
}
