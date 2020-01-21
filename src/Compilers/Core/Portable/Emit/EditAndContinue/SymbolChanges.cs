// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class SymbolChanges
    {
        /// <summary>
        /// Maps definitions being emitted to the corresponding definitions defined in the previous generation (metadata or source).
        /// </summary>
        private readonly DefinitionMap _definitionMap;

        /// <summary>
        /// Contains all symbols explicitly updated/added to the source and 
        /// their containing types and namespaces. 
        /// </summary>
        private readonly IReadOnlyDictionary<ISymbol, SymbolChange> _changes;

        private readonly Func<ISymbol, bool> _isAddedSymbol;

        protected SymbolChanges(DefinitionMap definitionMap, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol)
        {
            Debug.Assert(definitionMap != null);
            Debug.Assert(edits != null);
            Debug.Assert(isAddedSymbol != null);

            _definitionMap = definitionMap;
            _isAddedSymbol = isAddedSymbol;
            _changes = CalculateChanges(edits);
        }

        /// <summary>
        /// True if the symbol is a source symbol added during EnC session. 
        /// The symbol may be declared in any source compilation in the current solution.
        /// </summary>
        public bool IsAdded(ISymbol symbol)
        {
            return _isAddedSymbol(symbol);
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
            var synthesizedDef = def as ISynthesizedMethodBodyImplementationSymbol;
            if (synthesizedDef != null)
            {
                Debug.Assert(synthesizedDef.Method != null);

                var generator = synthesizedDef.Method;
                ISymbolInternal synthesizedSymbol = synthesizedDef;

                var change = GetChange((IDefinition)generator);
                switch (change)
                {
                    case SymbolChange.Updated:
                        // The generator has been updated. Some synthesized members should be reused, others updated or added.

                        // The container of the synthesized symbol doesn't exist, we need to add the symbol.
                        // This may happen e.g. for members of a state machine type when a non-iterator method is changed to an iterator.
                        if (!_definitionMap.DefinitionExists((IDefinition)synthesizedSymbol.ContainingType))
                        {
                            return SymbolChange.Added;
                        }

                        if (!_definitionMap.DefinitionExists(def))
                        {
                            // A method was changed to a method containing a lambda, to an iterator, or to an async method.
                            // The state machine or closure class has been added.
                            return SymbolChange.Added;
                        }

                        // The existing symbol should be reused when the generator is updated,
                        // not updated since it's form doesn't depend on the content of the generator.
                        // For example, when an iterator method changes all methods that implement IEnumerable 
                        // but MoveNext can be reused as they are.
                        if (!synthesizedDef.HasMethodBodyDependency)
                        {
                            return SymbolChange.None;
                        }

                        // If the type produced from the method body existed before then its members are updated.
                        if (synthesizedSymbol.Kind == SymbolKind.NamedType)
                        {
                            return SymbolChange.ContainsChanges;
                        }

                        if (synthesizedSymbol.Kind == SymbolKind.Method)
                        {
                            // The method body might have been updated.
                            return SymbolChange.Updated;
                        }

                        return SymbolChange.None;

                    case SymbolChange.Added:
                        // The method has been added - add the synthesized member as well, unless they already exist.
                        if (!_definitionMap.DefinitionExists(def))
                        {
                            return SymbolChange.Added;
                        }

                        // If the existing member is a type we need to add new members into it.
                        // An example is a shared static display class - an added method with static lambda will contribute
                        // the lambda and cache fields into the shared display class.
                        if (synthesizedSymbol.Kind == SymbolKind.NamedType)
                        {
                            return SymbolChange.ContainsChanges;
                        }

                        // Update method.
                        // An example is a constructor a shared display class - an added method with lambda will contribute
                        // cache field initialization code into the constructor.
                        if (synthesizedSymbol.Kind == SymbolKind.Method)
                        {
                            return SymbolChange.Updated;
                        }

                        // Otherwise, there is nothing to do.
                        // For example, a static lambda display class cache field.
                        return SymbolChange.None;

                    default:
                        // The method had to change, otherwise the synthesized symbol wouldn't be generated
                        throw ExceptionUtilities.UnexpectedValue(change);
                }
            }

            if (def is ISymbolInternal symbol)
            {
                return GetChange(symbol.GetISymbol());
            }

            // If the def existed in the previous generation, the def is unchanged
            // (although it may contain changed defs); otherwise, it was added.
            if (_definitionMap.DefinitionExists(def))
            {
                return (def is ITypeDefinition) ? SymbolChange.ContainsChanges : SymbolChange.None;
            }

            return SymbolChange.Added;
        }

        private SymbolChange GetChange(ISymbol symbol)
        {
            if (_changes.TryGetValue(symbol, out var change))
            {
                return change;
            }

            // Calculate change based on change to container.
            var container = GetContainingSymbol(symbol);
            if (container == null)
            {
                return SymbolChange.None;
            }

            var containerChange = GetChange(container);
            switch (containerChange)
            {
                case SymbolChange.Added:
                    // If container is added then all its members have been added.
                    return SymbolChange.Added;

                case SymbolChange.None:
                    // If container has no changes then none of its members have any changes.
                    return SymbolChange.None;

                case SymbolChange.Updated:
                case SymbolChange.ContainsChanges:
                    ISymbolInternal symbolInternal = GetISymbolInternalOrNull(symbol);

                    if (symbolInternal is IDefinition definition)
                    {
                        // If the definition did not exist in the previous generation, it was added.
                        return _definitionMap.DefinitionExists(definition) ? SymbolChange.None : SymbolChange.Added;
                    }

                    if (symbolInternal is INamespace @namespace)
                    {
                        // If the namespace did not exist in the previous generation, it was added.
                        // Otherwise the namespace may contain changes.
                        return _definitionMap.NamespaceExists(@namespace) ? SymbolChange.ContainsChanges : SymbolChange.Added;
                    }

                    return SymbolChange.None;

                default:
                    throw ExceptionUtilities.UnexpectedValue(containerChange);
            }
        }

        protected abstract ISymbolInternal GetISymbolInternalOrNull(ISymbol symbol);

        public IEnumerable<INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context)
        {
            foreach (var symbol in _changes.Keys)
            {
                var namespaceTypeDef = (GetISymbolInternalOrNull(symbol) as ITypeDefinition)?.AsNamespaceTypeDefinition(context);
                if (namespaceTypeDef != null)
                {
                    yield return namespaceTypeDef;
                }
            }
        }

        /// <summary>
        /// Calculate the set of changes up to top-level types. The result
        /// will be used as a filter when traversing the module.
        /// 
        /// Note that these changes only include user-defined source symbols, not synthesized symbols since those will be 
        /// generated during lowering of the changed user-defined symbols.
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

                AddContainingTypesAndNamespaces(changes, member);
                changes.Add(member, change);
            }

            return changes;
        }

        private static void AddContainingTypesAndNamespaces(Dictionary<ISymbol, SymbolChange> changes, ISymbol symbol)
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
