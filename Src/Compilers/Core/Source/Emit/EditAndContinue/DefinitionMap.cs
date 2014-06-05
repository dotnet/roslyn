// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class DefinitionMap
    {
        protected struct MethodDefinitionEntry
        {
            public MethodDefinitionEntry(IMethodSymbol previousMethod, bool preserveLocalVariables, Func<SyntaxNode, SyntaxNode> syntaxMap)
            {
                this.PreviousMethod = previousMethod;
                this.PreserveLocalVariables = preserveLocalVariables;
                this.SyntaxMap = syntaxMap;
            }

            public readonly IMethodSymbol PreviousMethod;
            public readonly bool PreserveLocalVariables;
            public readonly Func<SyntaxNode, SyntaxNode> SyntaxMap;
        }

        internal static readonly GetPreviousLocalSlot NoPreviousLocalSlot = (identity, type, constraints) => -1;

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
                            (IMethodSymbol)edit.OldSymbol,
                            edit.PreserveLocalVariables,
                            edit.SyntaxMap));
                    }
                }
            }

            return methodMap;
        }

        internal abstract bool TryGetTypeHandle(Cci.ITypeDefinition def, out TypeHandle handle);
        internal abstract bool TryGetEventHandle(Cci.IEventDefinition def, out EventHandle handle);
        internal abstract bool TryGetFieldHandle(Cci.IFieldDefinition def, out FieldHandle handle);
        internal abstract bool TryGetMethodHandle(Cci.IMethodDefinition def, out MethodHandle handle);
        internal abstract bool TryGetPropertyHandle(Cci.IPropertyDefinition def, out PropertyHandle handle);

        internal abstract bool TryGetPreviousLocals(
            EmitBaseline baseline,
            IMethodSymbol method,
            out ImmutableArray<EncLocalInfo> previousLocals,
            out GetPreviousLocalSlot getPreviousLocalSlot);

        internal abstract ImmutableArray<EncLocalInfo> GetLocalInfo(
            Cci.IMethodDefinition def,
            ImmutableArray<Cci.ILocalDefinition> localDefs,
            ImmutableArray<byte[]> signatures);

        internal abstract bool DefinitionExists(Cci.IDefinition def);
    }
}
