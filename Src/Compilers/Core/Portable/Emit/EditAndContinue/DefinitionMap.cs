// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class DefinitionMap
    {
        protected struct MethodDefinitionEntry
        {
            public MethodDefinitionEntry(IMethodSymbolInternal previousMethod, bool preserveLocalVariables, Func<SyntaxNode, SyntaxNode> syntaxMap)
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
        protected readonly IReadOnlyDictionary<IMethodSymbol, MethodDefinitionEntry> methodMap;

        protected DefinitionMap(PEModule module, IEnumerable<SemanticEdit> edits)
        {
            Debug.Assert(module != null);
            Debug.Assert(edits != null);

            this.module = module;
            this.methodMap = GenerateMethodMap(edits);
        }

        private static IReadOnlyDictionary<IMethodSymbol, MethodDefinitionEntry> GenerateMethodMap(IEnumerable<SemanticEdit> edits)
        {
            var methodMap = new Dictionary<IMethodSymbol, MethodDefinitionEntry>();
            foreach (var edit in edits)
            {
                if (edit.Kind == SemanticEditKind.Update)
                {
                    var method = edit.NewSymbol as IMethodSymbol;
                    if (method != null)
                    {
                        methodMap.Add(method, new MethodDefinitionEntry(
                            (IMethodSymbolInternal)edit.OldSymbol,
                            edit.PreserveLocalVariables,
                            edit.SyntaxMap));
                    }
                }
            }

            return methodMap;
        }

        internal abstract bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeDefinitionHandle handle);
        internal abstract bool TryGetEventHandle(Cci.IEventDefinition def, out EventDefinitionHandle handle);
        internal abstract bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldDefinitionHandle handle);
        internal abstract bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodDefinitionHandle handle);
        internal abstract bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyDefinitionHandle handle);

        internal abstract VariableSlotAllocator TryCreateVariableSlotAllocator(EmitBaseline baseline, IMethodSymbol method);

        internal ImmutableArray<EncLocalInfo> GetLocalInfo(Cci.IMethodDefinition methodDef, ImmutableArray<Cci.ILocalDefinition> localDefs, ImmutableArray<byte[]> signatures)
        {
            if (localDefs.IsEmpty)
            {
                return ImmutableArray<EncLocalInfo>.Empty;
            }

            return localDefs.SelectAsArray((localDef, i, _) => GetLocalInfo(localDef, signatures[i]), (object)null);
        }

        private static EncLocalInfo GetLocalInfo(Cci.ILocalDefinition localDef, byte[] signature)
        {
            if (localDef.Id.IsNone)
            {
                return new EncLocalInfo(signature);
            }

            return new EncLocalInfo(localDef.Id, localDef.Type, localDef.Constraints, localDef.Kind, signature);
        }

        internal abstract bool DefinitionExists(Cci.IDefinition def);

        protected static IReadOnlyDictionary<SyntaxNode, int> CreateDeclaratorToSyntaxOrdinalMap(ImmutableArray<SyntaxNode> declarators)
        {
            var declaratorToIndex = new Dictionary<SyntaxNode, int>();
            for (int i = 0; i < declarators.Length; i++)
            {
                declaratorToIndex.Add(declarators[i], i);
            }
            return declaratorToIndex;
        }
    }
}
