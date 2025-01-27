// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class SymbolChanges
    {
        /// <summary>
        /// Maps definitions being emitted to the corresponding definitions defined in the previous generation (metadata or source).
        /// </summary>
        private readonly DefinitionMap _definitionMap;

        /// <summary>
        /// Contains all symbols from the current compilation that were explicitly updated/added to the source and 
        /// their containing types and namespaces.
        /// </summary>
        private readonly IReadOnlyDictionary<ISymbolInternal, SymbolChange> _changes;

        /// <summary>
        /// A set of symbols whose name emitted to metadata must include a "#{generation}" suffix to avoid naming collisions with existing types.
        /// Populated based on semantic edits with <see cref="SemanticEditKind.Replace"/>.
        /// </summary>
        private readonly ISet<ISymbolInternal> _replacedSymbols;

        /// <summary>
        /// A set of symbols, from the old compilation, that have been deleted from the new compilation
        /// keyed by the containing type from the new compilation.
        /// Populated based on semantic edits with <see cref="SemanticEditKind.Delete"/>.
        /// </summary>
        public readonly IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> DeletedMembers;

        /// <summary>
        /// Updated methods.
        /// </summary>
        public readonly IReadOnlyDictionary<INamedTypeSymbolInternal, ImmutableArray<(IMethodSymbolInternal oldMethod, IMethodSymbolInternal newMethod)>> UpdatedMethods;

        private readonly Func<ISymbol, bool> _isAddedSymbol;

        protected SymbolChanges(DefinitionMap definitionMap, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol)
        {
            _definitionMap = definitionMap;
            _isAddedSymbol = isAddedSymbol;
            CalculateChanges(edits, out _changes, out _replacedSymbols, out DeletedMembers, out UpdatedMethods);
        }

        public DefinitionMap DefinitionMap => _definitionMap;

        public bool IsReplacedDef(IDefinition definition, bool checkEnclosingTypes = false)
            => definition.GetInternalSymbol() is { } internalSymbol && IsReplaced(internalSymbol, checkEnclosingTypes);

        public bool IsReplaced(ISymbolInternal symbol, bool checkEnclosingTypes = false)
        {
            ISymbolInternal? currentSymbol = symbol;

            while (currentSymbol != null)
            {
                if (_replacedSymbols.Contains(currentSymbol))
                {
                    return true;
                }

                if (!checkEnclosingTypes)
                {
                    return false;
                }

                currentSymbol = currentSymbol.ContainingType;
            }

            return false;
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
        public bool RequiresCompilation(ISymbolInternal symbol)
            => GetChange(symbol) != SymbolChange.None;

        private bool DefinitionExistsInPreviousGeneration(ISymbolInternal symbol)
        {
            var definition = (IDefinition)symbol.GetCciAdapter();

            if (!_definitionMap.DefinitionExists(definition))
            {
                return false;
            }

            // Definition map does not consider types that are being replaced,
            // hence we need to check - type that is being replaced is not considered
            // existing in the previous generation.
            var current = symbol;
            do
            {
                if (_replacedSymbols.Contains(current))
                {
                    return false;
                }

                current = current.ContainingType;
            }
            while (current is not null);

            return true;
        }

        public SymbolChange GetChange(IDefinition def)
        {
            var symbol = def.GetInternalSymbol();

            if (symbol is ISynthesizedGlobalMethodSymbol)
            {
                // Global methods are not reused, we always generate a new one.
                return SymbolChange.Added;
            }

            if (symbol is ISynthesizedMethodBodyImplementationSymbol synthesizedSymbol)
            {
                RoslynDebug.Assert(synthesizedSymbol.Method != null);

                var generatorChange = GetChange((IDefinition)synthesizedSymbol.Method.GetCciAdapter());
                switch (generatorChange)
                {
                    case SymbolChange.Updated:
                        // The generator has been updated. Some synthesized members should be reused, others updated or added.

                        // The container of the synthesized symbol doesn't exist, we need to add the symbol.
                        // This may happen e.g. for members of a state machine type when a non-iterator method is changed to an iterator.
                        if (!DefinitionExistsInPreviousGeneration(synthesizedSymbol.ContainingType))
                        {
                            return SymbolChange.Added;
                        }

                        if (!DefinitionExistsInPreviousGeneration(synthesizedSymbol))
                        {
                            // A method was changed to a method containing a lambda, to an iterator, or to an async method.
                            // The state machine or closure class has been added.
                            return SymbolChange.Added;
                        }

                        // The existing symbol should be reused when the generator is updated,
                        // not updated since it's form doesn't depend on the content of the generator.
                        // For example, when an iterator method changes all methods that implement IEnumerable 
                        // but MoveNext can be reused as they are.
                        if (!synthesizedSymbol.HasMethodBodyDependency)
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
                        // The method has been added - add the synthesized member as well, unless it already exists.
                        if (!DefinitionExistsInPreviousGeneration(synthesizedSymbol))
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
                        throw ExceptionUtilities.UnexpectedValue(generatorChange);
                }
            }

            if (symbol is not null)
            {
                return GetChange(symbol);
            }

            // If the def that has no associated internal symbol existed in the previous generation, the def is unchanged
            // (although it may contain changed defs); otherwise, it was added.
            if (_definitionMap.DefinitionExists(def))
            {
                return (def is ITypeDefinition) ? SymbolChange.ContainsChanges : SymbolChange.None;
            }

            return SymbolChange.Added;
        }

        private SymbolChange GetChange(ISymbolInternal symbol)
        {
            // In CalculateChanges we always store definitions for partial methods, so we have to
            // make sure we do the same thing here when we try to retrieve a change, as the compiler
            // associates synthesized methods with the implementation of the method that caused it
            // to be generated.
            if (symbol is IMethodSymbolInternal method)
            {
                symbol = method.PartialDefinitionPart ?? symbol;
            }

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
                    if (symbol.Kind == SymbolKind.Namespace)
                    {
                        // If the namespace did not exist in the previous generation, it was added.
                        // Otherwise the namespace may contain changes.
                        return _definitionMap.NamespaceExists((INamespace)symbol.GetCciAdapter()) ? SymbolChange.ContainsChanges : SymbolChange.Added;
                    }

                    // If the definition did not exist in the previous generation, it was added.
                    return DefinitionExistsInPreviousGeneration(symbol) ? SymbolChange.None : SymbolChange.Added;

                default:
                    throw ExceptionUtilities.UnexpectedValue(containerChange);
            }
        }

        public SymbolChange GetChangeForPossibleReAddedMember(ITypeDefinitionMember item, Func<ITypeDefinitionMember, bool> definitionExistsInAnyPreviousGeneration)
        {
            var change = GetChange(item);

            return fixChangeIfMemberIsReAdded(item, change, definitionExistsInAnyPreviousGeneration);

            SymbolChange fixChangeIfMemberIsReAdded(ITypeDefinitionMember item, SymbolChange change, Func<ITypeDefinitionMember, bool> definitionExistsInAnyPreviousGeneration)
            {
                // If this is a field that is being added, but it's part of a property or event that has been deleted
                // and is now being re-added, we don't want to add the field twice, so we ignore the change.
                // Unlike properties and methods, since we can't replace a field with a MissingMethodException
                // we don't need to update it at all.
                // This also makes sure to check that the field itself is being re-added, because it could be
                // a property that is being re-added as an auto-prop, when it wasn't one before, for example.
                if (item is IFieldDefinition fieldDefinition &&
                    GetContainingDefinitionForBackingField(fieldDefinition) is ITypeDefinitionMember containingDef &&
                    GetChange(containingDef) == SymbolChange.Added &&
                    definitionExistsInAnyPreviousGeneration(item) &&
                    fixChangeIfMemberIsReAdded(containingDef, SymbolChange.Added, definitionExistsInAnyPreviousGeneration) == SymbolChange.Updated)
                {
                    return SymbolChange.None;
                }

                // Otherwise if the item was added, and not replaced, but we can find an existing row id, then treat it
                // as an update. This supercedes the other checks for edit types etc. because a method could be
                // deleted in a generation, and then "added" in a subsequent one, but that is an update
                // even if the previous generation doesn't know about it.
                if (change == SymbolChange.Added &&
                    !IsReplacedDef(item.ContainingTypeDefinition, checkEnclosingTypes: true) &&
                    definitionExistsInAnyPreviousGeneration(item))
                {
                    return SymbolChange.Updated;
                }

                return change;
            }
        }

        protected abstract ISymbolInternal? GetISymbolInternalOrNull(ISymbol symbol);

        public ISymbolInternal GetRequiredInternalSymbol(ISymbol? symbol)
        {
            Debug.Assert(symbol != null);
            var result = GetISymbolInternalOrNull(symbol);
            Debug.Assert(result != null);
            return result;
        }

        public IEnumerable<INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context)
        {
            foreach (var (symbol, _) in _changes)
            {
                var namespaceTypeDef = (symbol.GetCciAdapter() as ITypeDefinition)?.AsNamespaceTypeDefinition(context);
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
        private void CalculateChanges(
            IEnumerable<SemanticEdit> edits,
            out IReadOnlyDictionary<ISymbolInternal, SymbolChange> changes,
            out ISet<ISymbolInternal> replacedSymbols,
            out IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> deletedMembers,
            out IReadOnlyDictionary<INamedTypeSymbolInternal, ImmutableArray<(IMethodSymbolInternal oldMethod, IMethodSymbolInternal newMethod)>> updatedMethods)
        {
            var changesBuilder = new Dictionary<ISymbolInternal, SymbolChange>();
            var updatedMethodsBuilder = new Dictionary<INamedTypeSymbolInternal, ArrayBuilder<(IMethodSymbolInternal oldMethod, IMethodSymbolInternal newMethod)>>();
            var lazyReplacedSymbolsBuilder = (HashSet<ISymbolInternal>?)null;
            var lazyDeletedMembersBuilder = (Dictionary<ISymbolInternal, ArrayBuilder<ISymbolInternal>>?)null;

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

                    case SemanticEditKind.Replace:
                        (lazyReplacedSymbolsBuilder ??= new HashSet<ISymbolInternal>()).Add(GetRequiredInternalSymbol(edit.NewSymbol));
                        change = SymbolChange.Added;
                        break;

                    case SemanticEditKind.Delete:
                        Debug.Assert(edit.OldSymbol is IMethodSymbol or IPropertySymbol or IEventSymbol);

                        // For deletions NewSymbol is actually containing symbol
                        var newContainingType = (INamedTypeSymbolInternal)GetRequiredInternalSymbol(edit.NewSymbol);

                        lazyDeletedMembersBuilder ??= new();
                        if (!lazyDeletedMembersBuilder.TryGetValue(newContainingType, out var deletedMembersPerType))
                        {
                            deletedMembersPerType = ArrayBuilder<ISymbolInternal>.GetInstance();
                            lazyDeletedMembersBuilder.Add(newContainingType, deletedMembersPerType);
                        }

                        var oldSymbol = GetRequiredInternalSymbol(edit.OldSymbol);

                        // edited symbols must be unique:
                        Debug.Assert(!deletedMembersPerType.Contains(oldSymbol));
                        deletedMembersPerType.Add(oldSymbol);

                        // We need to make sure we track the containing type of the member being
                        // deleted, from the new compilation, in case the deletion is the only change.
                        if (!changesBuilder.ContainsKey(newContainingType))
                        {
                            changesBuilder.Add(newContainingType, SymbolChange.ContainsChanges);
                            AddContainingSymbolChanges(changesBuilder, newContainingType);
                        }

                        continue;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                }

                var newMember = GetRequiredInternalSymbol(edit.NewSymbol);

                // Partial methods/properties/indexers are supplied as implementations but recorded
                // internally as definitions since definitions are used in emit.
                if (newMember.Kind == SymbolKind.Method)
                {
                    var newMethod = (IMethodSymbolInternal)newMember;

                    // Partial methods should be implementations, not definitions.
                    Debug.Assert(newMethod.PartialImplementationPart == null);
                    Debug.Assert(edit.OldSymbol == null || ((IMethodSymbol)edit.OldSymbol).PartialImplementationPart == null);

                    newMember = newMethod.PartialDefinitionPart ?? newMember;

                    if (edit.Kind == SemanticEditKind.Update)
                    {
                        var oldMethod = (IMethodSymbolInternal)GetRequiredInternalSymbol(edit.OldSymbol);

                        if (!updatedMethodsBuilder.TryGetValue(newMember.ContainingType, out var updatedMethodsPerType))
                        {
                            updatedMethodsPerType = ArrayBuilder<(IMethodSymbolInternal, IMethodSymbolInternal)>.GetInstance();
                            updatedMethodsBuilder.Add(newMember.ContainingType, updatedMethodsPerType);
                        }

                        updatedMethodsPerType.Add((oldMethod.PartialDefinitionPart ?? oldMethod, (IMethodSymbolInternal)newMember));
                    }
                }
                else if (newMember.Kind == SymbolKind.Property)
                {
                    var newProperty = (IPropertySymbolInternal)newMember;

                    // Partial properties should be implementations, not definitions.
                    Debug.Assert(newProperty.PartialImplementationPart == null);
                    Debug.Assert(edit.OldSymbol == null || ((IPropertySymbol)edit.OldSymbol).PartialImplementationPart == null);

                    newMember = newProperty.PartialDefinitionPart ?? newMember;
                }

                AddContainingSymbolChanges(changesBuilder, newMember);

                // If we saw an edit for a symbol that is contained in the current symbol, we would have already flagged it as "containing changes".
                // If so we "upgrade" the change to the one requested by semantic edit.
                if (changesBuilder.TryGetValue(newMember, out var existingChange) && existingChange == SymbolChange.ContainsChanges)
                {
                    changesBuilder[newMember] = change;
                }
                else
                {
                    changesBuilder.Add(newMember, change);
                }
            }

            changes = changesBuilder;
            replacedSymbols = lazyReplacedSymbolsBuilder ?? SpecializedCollections.EmptySet<ISymbolInternal>();

            deletedMembers = lazyDeletedMembersBuilder?.ToImmutableSegmentedDictionary(
                keySelector: static e => e.Key,
                elementSelector: static e => e.Value.ToImmutableAndFree()) ?? ImmutableSegmentedDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>>.Empty;

            updatedMethods = updatedMethodsBuilder.ToImmutableSegmentedDictionary(
               keySelector: static e => e.Key,
               elementSelector: static e => e.Value.ToImmutableAndFree());
        }

        private static void AddContainingSymbolChanges(Dictionary<ISymbolInternal, SymbolChange> changes, ISymbolInternal symbol)
        {
            while (true)
            {
                var containingSymbol = GetContainingSymbol(symbol);
                if (containingSymbol == null || changes.ContainsKey(containingSymbol))
                {
                    return;
                }

                changes.Add(containingSymbol, SymbolChange.ContainsChanges);
                symbol = containingSymbol;
            }
        }

        /// <summary>
        /// Return the symbol that contains this symbol as far
        /// as changes are concerned. For instance, an auto property
        /// is considered the containing symbol for the backing
        /// field and the accessor methods. By default, the containing
        /// symbol is simply Symbol.ContainingSymbol.
        /// </summary>
        private static ISymbolInternal? GetContainingSymbol(ISymbolInternal symbol)
        {
            // This approach of walking up the symbol hierarchy towards the
            // root, rather than walking down to the leaf symbols, seems
            // unreliable. It may be better to walk down using the usual
            // emit traversal, but prune the traversal to those types and
            // members that are known to contain changes.
            var associated = GetAssociatedSymbol(symbol);
            if (associated is not null)
            {
                return associated;
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

        private static ISymbolInternal? GetAssociatedSymbol(ISymbolInternal symbol)
            => symbol switch
            {
                IFieldSymbolInternal field => field.AssociatedSymbol,
                IMethodSymbolInternal method => method.AssociatedSymbol,
                _ => null
            };

        internal IDefinition? GetContainingDefinitionForBackingField(IFieldDefinition fieldDefinition)
            => fieldDefinition.GetInternalSymbol() is { } fieldSymbol ? GetAssociatedSymbol(fieldSymbol)?.GetCciAdapter() as IDefinition : null;
    }
}
