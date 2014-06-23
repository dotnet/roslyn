// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class SymbolChanges
    {
        private readonly DefinitionMap definitionMap;
        private readonly IReadOnlyDictionary<ISymbol, SymbolChange> changes;

        public SymbolChanges(DefinitionMap definitionMap, IEnumerable<SemanticEdit> edits)
        {
            Debug.Assert(definitionMap != null);
            Debug.Assert(edits != null);
            this.definitionMap = definitionMap;
            this.changes = CalculateChanges(edits);
        }

        /// <summary>
        /// Returns true if the symbol or some child symbol has changed and needs to be compiled.
        /// </summary>
        public bool RequiresCompilation(ISymbol symbol)
        {
            return this.GetChange(symbol) != SymbolChange.None;
        }

        public SymbolChange GetChange(IDefinition def)
        {
            var symbol = def as ISymbol;
            if (symbol != null)
            {
                return GetChange(symbol);
            }

            // If the def existed in the previous generation, the def is unchanged
            // (although it may contain changed defs); otherwise, it was added.
            if (this.definitionMap.DefinitionExists(def))
            {
                var typeDef = def as ITypeDefinition;
                return (typeDef != null) ? SymbolChange.ContainsChanges : SymbolChange.None;
            }
            return SymbolChange.Added;
        }

        private SymbolChange GetChange(ISymbol symbol)
        {
            SymbolChange change;
            if (this.changes.TryGetValue(symbol, out change))
            {
                return change;
            }

            // Calculate change based on change to container.
            var container = GetContainingSymbol(symbol);
            if (container == null)
            {
                return SymbolChange.None;
            }

            switch (this.GetChange(container))
            {
                case SymbolChange.Added:
                    return SymbolChange.Added;

                case SymbolChange.None:
                    return SymbolChange.None;

                case SymbolChange.Updated:
                case SymbolChange.ContainsChanges:
                    var definition = symbol as IDefinition;

                    if (definition != null && !this.definitionMap.DefinitionExists(definition))
                    {
                        // If the definition did not exist in the previous generation, it was added.
                        return SymbolChange.Added;
                    }

                    return SymbolChange.None;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public IEnumerable<INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
        {
            var module = (CommonPEModuleBuilder)context.Module;
            foreach (var type in module.GetAnonymousTypes())
            {
                yield return type;
            }

            foreach (var symbol in this.changes.Keys)
            {
                var typeDef = symbol as ITypeDefinition;
                if (typeDef != null)
                {
                    var namespaceTypeDef = typeDef.AsNamespaceTypeDefinition(context);
                    if (namespaceTypeDef != null)
                    {
                        yield return namespaceTypeDef;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the set of changes up to top-level types. The result
        /// will be used as a filter when traversing the module.
        /// </summary>
        private static IReadOnlyDictionary<ISymbol, SymbolChange> CalculateChanges(IEnumerable<SemanticEdit> edits)
        {
            var changes = new Dictionary<ISymbol, SymbolChange>();

            foreach (var edit in edits)
            {
                SymbolChange change;

                switch (edit.Kind)
                {
                    case SemanticEditKind.Update:
                        change = SymbolChange.Updated;
                        break;

                    case SemanticEditKind.Insert:
                        change = SymbolChange.Added;
                        break;

                    case SemanticEditKind.Delete:
                        // No work to do.
                        continue;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                }

                var member = edit.NewSymbol;

                // Partial methods are supplied as implementations but recorded
                // internally as definitions since definitions are used in emit.
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)member;

                    // Partial methods should be implementations, not definitions.
                    Debug.Assert(method.PartialImplementationPart == null);
                    Debug.Assert((edit.OldSymbol == null) || (((IMethodSymbol)edit.OldSymbol).PartialImplementationPart == null));

                    var definitionPart = method.PartialDefinitionPart;
                    if (definitionPart != null)
                    {
                        member = definitionPart;
                    }
                }

                AddContainingTypes(changes, member);
                changes.Add(member, change);
            }

            return changes;
        }

        private static void AddContainingTypes(Dictionary<ISymbol, SymbolChange> changes, ISymbol symbol)
        {
            while (true)
            {
                symbol = GetContainingSymbol(symbol);
                if (symbol == null)
                {
                    return;
                }

                if (changes.ContainsKey(symbol))
                {
                    return;
                }

                var kind = symbol.Kind;
                if (kind == SymbolKind.Property || kind == SymbolKind.Event)
                {
                    changes.Add(symbol, SymbolChange.Updated);
                }
                else
                {
                    changes.Add(symbol, SymbolChange.ContainsChanges);
                }
            }
        }

        /// <summary>
        /// Return the symbol that contains this symbol as far
        /// as changes are concerned. For instance, an auto property
        /// is considered the containing symbol for the backing
        /// field and the accessor methods. By default, the containing
        /// symbol is simply Symbol.ContainingSymbol.
        /// </summary>
        private static ISymbol GetContainingSymbol(ISymbol symbol)
        {
            // This approach of walking up the symbol hierarchy towards the
            // root, rather than walking down to the leaf symbols, seems
            // unreliable. It may be better to walk down using the usual
            // emit traversal, but prune the traversal to those types and
            // members that are known to contain changes.
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    {
                        var associated = ((IFieldSymbol)symbol).AssociatedSymbol;
                        if (associated != null)
                        {
                            return associated;
                        }
                    }
                    break;
                case SymbolKind.Method:
                    {
                        var associated = ((IMethodSymbol)symbol).AssociatedSymbol;
                        if (associated != null)
                        {
                            return associated;
                        }
                    }
                    break;
            }

            symbol = symbol.ContainingSymbol;
            if (symbol != null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.NetModule:
                    case SymbolKind.Assembly:
                        // These symbols are never part of the changes collection.
                        return null;
                }
            }

            return symbol;
        }
    }
}
