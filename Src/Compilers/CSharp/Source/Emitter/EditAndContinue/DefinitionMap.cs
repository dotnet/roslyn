// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal struct MethodDefinitionEntry
    {
        public MethodDefinitionEntry(MethodSymbol previousMethod, bool preserveLocalVariables, Func<SyntaxNode, SyntaxNode> syntaxMap)
        {
            this.PreviousMethod = previousMethod;
            this.PreserveLocalVariables = preserveLocalVariables;
            this.SyntaxMap = syntaxMap;
        }

        public readonly MethodSymbol PreviousMethod;
        public readonly bool PreserveLocalVariables;
        public readonly Func<SyntaxNode, SyntaxNode> SyntaxMap;
    }

    /// <summary>
    /// Matches symbols from an assembly in one compilation to
    /// the corresponding assembly in another. Assumes that only
    /// one assembly has changed between the two compilations.
    /// </summary>
    internal sealed class DefinitionMap : Microsoft.CodeAnalysis.Emit.DefinitionMap
    {
        private readonly PEModule module;
        private readonly MetadataDecoder metadataDecoder;
        private readonly SymbolMatcher mapToMetadata;
        private readonly SymbolMatcher mapToPrevious;
        private readonly IReadOnlyDictionary<MethodSymbol, MethodDefinitionEntry> methodMap;

        public DefinitionMap(
            PEModule module,
            MetadataDecoder metadataDecoder,
            SymbolMatcher mapToMetadata,
            SymbolMatcher mapToPrevious,
            IReadOnlyDictionary<MethodSymbol, MethodDefinitionEntry> methodMap)
        {
            Debug.Assert(module != null);
            Debug.Assert(metadataDecoder != null);
            Debug.Assert(mapToMetadata != null);
            Debug.Assert(methodMap != null);

            this.module = module;
            this.metadataDecoder = metadataDecoder;
            this.mapToMetadata = mapToMetadata;
            this.mapToPrevious = mapToPrevious ?? mapToMetadata;
            this.methodMap = methodMap;
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
            Microsoft.CodeAnalysis.Emit.EmitBaseline baseline,
            IMethodSymbol method,
            out ImmutableArray<Microsoft.CodeAnalysis.Emit.EncLocalInfo> previousLocals,
            out GetPreviousLocalSlot getPreviousLocalSlot)
        {
            previousLocals = default(ImmutableArray<Microsoft.CodeAnalysis.Emit.EncLocalInfo>);
            getPreviousLocalSlot = NoPreviousLocalSlot;

            MethodHandle handle;
            if (!this.TryGetMethodHandle(baseline, (Cci.IMethodDefinition)method, out handle))
            {
                // Unrecognized method. Must have been added in the current compilation.
                return false;
            }

            MethodDefinitionEntry methodEntry;
            if (!this.methodMap.TryGetValue((MethodSymbol)method, out methodEntry))
            {
                // Not part of changeset. No need to preserve locals.
                return false;
            }

            if (!methodEntry.PreserveLocalVariables)
            {
                // Not necessary to preserve locals.
                return false;
            }

            var previousMethod = (MethodSymbol)methodEntry.PreviousMethod;
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
                    var methodIL = this.module.GetMethodILOrThrow(handle);

                    if (!methodIL.LocalSignature.IsNil)
                    {
                        var signature = this.module.MetadataReader.GetLocalSignature(methodIL.LocalSignature);
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
            var previousDeclarators = GetLocalVariableDeclaratorsVisitor.GetDeclarators(previousMethod);

            // Create a map from declarator to declarator offset.
            var previousDeclaratorToOffset = new Dictionary<SyntaxNode, int>();
            for (int offset = 0; offset < previousDeclarators.Length; offset++)
            {
                previousDeclaratorToOffset.Add(previousDeclarators[offset], offset);
            }

            // Create a map from local info to slot.
            var previousLocalInfoToSlot = new Dictionary<Microsoft.CodeAnalysis.Emit.EncLocalInfo, int>();
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
                var currentDeclarators = GetLocalVariableDeclaratorsVisitor.GetDeclarators((MethodSymbol)method);
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
                                    var localKey = new Microsoft.CodeAnalysis.Emit.EncLocalInfo(offset, previousType, constraints, (int)local.TempKind);
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

        private bool TryGetMethodHandle(Microsoft.CodeAnalysis.Emit.EmitBaseline baseline, Cci.IMethodDefinition def, out MethodHandle handle)
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

        internal override ImmutableArray<Microsoft.CodeAnalysis.Emit.EncLocalInfo> GetLocalInfo(
            Cci.IMethodDefinition methodDef,
            ImmutableArray<LocalDefinition> localDefs)
        {
            if (localDefs.IsEmpty)
            {
                return ImmutableArray<Microsoft.CodeAnalysis.Emit.EncLocalInfo>.Empty;
            }

            // Find declarators in current method syntax.
            var declarators = GetLocalVariableDeclaratorsVisitor.GetDeclarators((MethodSymbol)methodDef);

            // Create a map from declarator to declarator index.
            var declaratorToIndex = CreateDeclaratorToIndexMap(declarators);

            return localDefs.SelectAsArray(localDef => GetLocalInfo(declaratorToIndex, localDef));
        }

        private static Microsoft.CodeAnalysis.Emit.EncLocalInfo GetLocalInfo(
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
                    return new Microsoft.CodeAnalysis.Emit.EncLocalInfo(offset, localDef.Type, localDef.Constraints, (int)local.TempKind);
                }
            }

            return new Microsoft.CodeAnalysis.Emit.EncLocalInfo(localDef.Type, localDef.Constraints);
        }

        /// <summary>
        /// Match local declarations to names to generate a map from
        /// declaration to local slot. The names are indexed by slot and the
        /// assumption is that declarations are in the same order as slots.
        /// </summary>
        private static ImmutableArray<Microsoft.CodeAnalysis.Emit.EncLocalInfo> GetLocalSlots(
            MethodSymbol method,
            ImmutableArray<string> localNames,
            ImmutableArray<MetadataDecoder.LocalInfo> localInfo)
        {
            var syntaxRefs = method.DeclaringSyntaxReferences;

            // No syntax refs for synthesized methods.
            if (syntaxRefs.Length == 0)
            {
                return ImmutableArray<Microsoft.CodeAnalysis.Emit.EncLocalInfo>.Empty;
            }

            var syntax = syntaxRefs[0].GetSyntax();
            var map = new Dictionary<Microsoft.CodeAnalysis.Emit.EncLocalInfo, int>();
            var visitor = new GetLocalsVisitor(localNames, localInfo, map);
            visitor.Visit(syntax);
            var locals = new Microsoft.CodeAnalysis.Emit.EncLocalInfo[localInfo.Length];
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
                    locals[i] = new Microsoft.CodeAnalysis.Emit.EncLocalInfo((Cci.ITypeReference)info.Type, constraints);
                }
            }

            return ImmutableArray.Create(locals);
        }

        private static LocalSlotConstraints GetConstraints(MetadataDecoder.LocalInfo info)
        {
            return (info.IsPinned ? LocalSlotConstraints.Pinned : LocalSlotConstraints.None) |
                (info.IsByRef ? LocalSlotConstraints.ByRef : LocalSlotConstraints.None);
        }

        private sealed class GetLocalsVisitor : LocalVariableDeclaratorsVisitor
        {
            private readonly ImmutableArray<LocalName> localNames;
            private readonly ImmutableArray<MetadataDecoder.LocalInfo> localInfo;
            private readonly Dictionary<Microsoft.CodeAnalysis.Emit.EncLocalInfo, int> locals;
            private int slotIndex;
            private int offset;

            public GetLocalsVisitor(
                ImmutableArray<string> localNames,
                ImmutableArray<MetadataDecoder.LocalInfo> localInfo,
                Dictionary<Microsoft.CodeAnalysis.Emit.EncLocalInfo, int> locals)
            {
                this.localNames = localNames.SelectAsArray(ParseName);
                this.localInfo = localInfo;
                this.locals = locals;
                this.slotIndex = 0;
            }

            protected override void VisitFixedStatementDeclarations(FixedStatementSyntax node)
            {
                // Expecting N variable locals followed by N temporaries.
                var declarators = node.Declaration.Variables;
                int n = declarators.Count;
                int startOffset = this.offset;
                for (int i = 0; i < n; i++)
                {
                    var declarator = declarators[i];
                    TryGetSlotIndex(declarator.Identifier.ValueText);
                    this.offset++;
                }

                int endOffset = this.offset;
                this.offset = startOffset;
                for (int i = 0; i < n; i++)
                {
                    var declarator = declarators[i];
                    if (!IsSlotIndex(TempKind.FixedString))
                    {
                        break;
                    }
                    AddLocal(TempKind.FixedString);
                    this.offset++;
                }

                Debug.Assert(this.offset <= endOffset);
                this.offset = endOffset;
            }

            protected override void VisitForEachStatementDeclarations(ForEachStatementSyntax node)
            {
                // Expecting two or more locals: one for the enumerator,
                // for arrays one local for each upper bound and one for
                // each index, and finally one local for the loop variable.
                var kindOpt = TryGetSlotIndex(TempKind.ForEachEnumerator, TempKind.ForEachArray);
                if (kindOpt != null)
                {
                    // Enumerator.
                    if (kindOpt.Value == TempKind.ForEachArray)
                    {
                        // Upper bounds.
                        var kind = TempKind.ForEachArrayLimit0;
                        while (IsSlotIndex(kind))
                        {
                            AddLocal(kind);
                            kind = (TempKind)((int)kind + 1);
                        }

                        // Indices.
                        kind = TempKind.ForEachArrayIndex0;
                        while (IsSlotIndex(kind))
                        {
                            AddLocal(kind);
                            kind = (TempKind)((int)kind + 1);
                        }
                    }

                    // Loop variable.
                    string name = ((ForEachStatementSyntax)node).Identifier.ValueText;
                    if (IsSlotIndex(name))
                    {
                        AddLocal(TempKind.None);
                    }
                    else
                    {
                        // TODO: Handle missing temporary.
                    }
                }

                this.offset++;
            }

            protected override void VisitLockStatementDeclarations(LockStatementSyntax node)
            {
                // Expecting one or two locals depending on which overload of Monitor.Enter is used.
                var expr = node.Expression;
                Debug.Assert(expr != null);
                if (TryGetSlotIndex(TempKind.Lock) != null)
                {
                    // If the next local is LockTaken, then the lock was emitted with the two argument
                    // overload for Monitor.Enter(). Otherwise, the single argument overload was used.
                    if (IsSlotIndex(TempKind.LockTaken))
                    {
                        AddLocal(TempKind.LockTaken);
                    }
                }

                this.offset++;
            }

            protected override void VisitUsingStatementDeclarations(UsingStatementSyntax node)
            {
                // Expecting one temporary for using statement with no explicit declaration.
                if (node.Declaration == null)
                {
                    var expr = node.Expression;
                    Debug.Assert(expr != null);
                    TryGetSlotIndex(TempKind.Using);

                    this.offset++;
                }
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                TryGetSlotIndex(node.Identifier.ValueText);
                this.offset++;
            }

            private static LocalName ParseName(string name)
            {
                if (name == null)
                {
                    return new LocalName(name, TempKind.None, 0);
                }

                TempKind kind;
                int uniqueId;
                GeneratedNames.TryParseTemporaryName(name, out kind, out uniqueId);
                return new LocalName(name, kind, uniqueId);
            }

            private bool IsSlotIndex(string name)
            {
                return (this.slotIndex < this.localNames.Length) &&
                    (this.localNames[this.slotIndex].Kind == TempKind.None) &&
                    (name == this.localNames[this.slotIndex].Name);
            }

            private bool IsSlotIndex(params TempKind[] kinds)
            {
                Debug.Assert(Array.IndexOf(kinds, TempKind.None) < 0);

                return (this.slotIndex < this.localNames.Length) &&
                    (Array.IndexOf(kinds, this.localNames[this.slotIndex].Kind) >= 0);
            }

            private bool TryGetSlotIndex(string name)
            {
                while (this.slotIndex < this.localNames.Length)
                {
                    if (IsSlotIndex(name))
                    {
                        AddLocal(TempKind.None);
                        return true;
                    }
                    this.slotIndex++;
                }

                return false;
            }

            private TempKind? TryGetSlotIndex(params TempKind[] kinds)
            {
                while (this.slotIndex < this.localNames.Length)
                {
                    if (IsSlotIndex(kinds))
                    {
                        var localName = this.localNames[this.slotIndex];
                        var kind = localName.Kind;
                        AddLocal(kind);
                        return kind;
                    }
                    this.slotIndex++;
                }

                return null;
            }

            private void AddLocal(TempKind tempKind)
            {
                var info = this.localInfo[this.slotIndex];

                // We do not emit custom modifiers on locals so ignore the
                // previous version of the local if it had custom modifiers.
                if (info.CustomModifiers.IsDefaultOrEmpty)
                {
                    var constraints = GetConstraints(info);
                    var local = new Microsoft.CodeAnalysis.Emit.EncLocalInfo(this.offset, (Cci.ITypeReference)info.Type, constraints, (int)tempKind);
                    this.locals.Add(local, this.slotIndex);
                }

                this.slotIndex++;
            }

            [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
            private struct LocalName
            {
                public readonly string Name;
                public readonly TempKind Kind;
                public readonly int UniqueId;

                public LocalName(string name, TempKind kind, int uniqueId)
                {
                    this.Name = name;
                    this.Kind = kind;
                    this.UniqueId = uniqueId;
                }

                private string GetDebuggerDisplay()
                {
                    return string.Format("[{0}, {1}, {2}]", this.Kind, this.UniqueId, this.Name);
                }
            }
        }
    }

    internal sealed class GetLocalVariableDeclaratorsVisitor : LocalVariableDeclaratorsVisitor
    {
        internal static ImmutableArray<SyntaxNode> GetDeclarators(MethodSymbol method)
        {
            var syntaxRefs = method.DeclaringSyntaxReferences;
            // No syntax refs for synthesized methods.
            if (syntaxRefs.Length == 0)
            {
                return ImmutableArray<SyntaxNode>.Empty;
            }

            var syntax = syntaxRefs[0].GetSyntax();
            var builder = ArrayBuilder<SyntaxNode>.GetInstance();
            var visitor = new GetLocalVariableDeclaratorsVisitor(builder);
            visitor.Visit(syntax);
            return builder.ToImmutableAndFree();
        }

        private readonly ArrayBuilder<SyntaxNode> builder;

        public GetLocalVariableDeclaratorsVisitor(ArrayBuilder<SyntaxNode> builder)
        {
            this.builder = builder;
        }

        protected override void VisitFixedStatementDeclarations(FixedStatementSyntax node)
        {
            foreach (var declarator in node.Declaration.Variables)
            {
                this.builder.Add(declarator);
            }
        }

        protected override void VisitForEachStatementDeclarations(ForEachStatementSyntax node)
        {
            this.builder.Add(node);
        }

        protected override void VisitLockStatementDeclarations(LockStatementSyntax node)
        {
            var expr = node.Expression;
            Debug.Assert(expr != null);
            this.builder.Add(expr);
        }

        protected override void VisitUsingStatementDeclarations(UsingStatementSyntax node)
        {
            if (node.Declaration == null)
            {
                var expr = node.Expression;
                Debug.Assert(expr != null);

                this.builder.Add(expr);
            }
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            this.builder.Add(node);
        }
    }
}
