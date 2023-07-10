// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Cci;
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
        /// Contains all symbols explicitly updated/added to the source and 
        /// their containing types and namespaces. 
        /// </summary>
        private readonly IReadOnlyDictionary<ISymbol, SymbolChange> _changes;

        /// <summary>
        /// A set of symbols whose name emitted to metadata must include a "#{generation}" suffix to avoid naming collisions with existing types.
        /// Populated based on semantic edits with <see cref="SemanticEditKind.Replace"/>.
        /// </summary>
        private readonly ISet<ISymbol> _replacedSymbols;

        /// <summary>
        /// A set of symbols, from the old compilation, that have been deleted from the new compilation
        /// keyed by the containing type from the new compilation.
        /// Populated based on semantic edits with <see cref="SemanticEditKind.Delete"/>.
        /// </summary>
        private readonly IReadOnlyDictionary<ISymbol, ISet<ISymbol>> _deletedMembers;

        private readonly Func<ISymbol, bool> _isAddedSymbol;

        protected SymbolChanges(DefinitionMap definitionMap, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol)
        {
            _definitionMap = definitionMap;
            _isAddedSymbol = isAddedSymbol;
            CalculateChanges(edits, out _changes, out _replacedSymbols, out _deletedMembers);
        }

        public DefinitionMap DefinitionMap => _definitionMap;

        public ImmutableDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> GetAllDeletedMembers()
        {
            var builder = ImmutableDictionary.CreateBuilder<ISymbolInternal, ImmutableArray<ISymbolInternal>>();

            foreach (var (type, deletedMembers) in _deletedMembers)
            {
                if (GetISymbolInternalOrNull(type) is not { } typeSymbol)
                {
                    continue;
                }

                var internalSymbols = GetDeletedMemberInternalSymbols(deletedMembers, includeMethods: true, includeProperties: true, includeEvents: true);

                builder.Add(typeSymbol, internalSymbols);

            }

            return builder.ToImmutable();
        }

        private ImmutableArray<ISymbolInternal> GetDeletedMemberInternalSymbols(IDefinition containingType, bool includeMethods, bool includeProperties, bool includeEvents)
        {
            var containingSymbol = containingType.GetInternalSymbol()?.GetISymbol();
            if (containingSymbol is null)
            {
                return ImmutableArray<ISymbolInternal>.Empty;
            }

            if (!_deletedMembers.TryGetValue(containingSymbol, out var deleted))
            {
                return ImmutableArray<ISymbolInternal>.Empty;
            }

            return GetDeletedMemberInternalSymbols(deleted, includeMethods, includeProperties, includeEvents);
        }

        private ImmutableArray<ISymbolInternal> GetDeletedMemberInternalSymbols(ISet<ISymbol> deletedMembers, bool includeMethods, bool includeProperties, bool includeEvents)
        {
            var internalSymbols = ArrayBuilder<ISymbolInternal>.GetInstance();

            foreach (var symbol in deletedMembers)
            {
                if (GetISymbolInternalOrNull(symbol) is { } internalSymbol)
                {
                    if (includeProperties &&
                        symbol is IMethodSymbol { AssociatedSymbol: IPropertySymbol propertySymbol } &&
                        (propertySymbol.GetMethod is null || propertySymbol.GetMethod == symbol))
                    {
                        var internalPropertySymbol = GetISymbolInternalOrNull(propertySymbol);
                        if (internalPropertySymbol is not null)
                        {
                            internalSymbols.Add(internalPropertySymbol);
                        }
                    }

                    if (includeEvents &&
                        symbol is IMethodSymbol { AssociatedSymbol: IEventSymbol eventSymbol } &&
                        eventSymbol.AddMethod == symbol)
                    {
                        var internalEventSymbol = GetISymbolInternalOrNull(eventSymbol);
                        if (internalEventSymbol is not null)
                        {
                            internalSymbols.Add(internalEventSymbol);
                        }
                    }

                    if (includeMethods &&
                        symbol is IMethodSymbol)
                    {
                        internalSymbols.Add(internalSymbol);
                    }
                }
            }

            return internalSymbols.ToImmutableAndFree();
        }

        public ImmutableArray<ISymbolInternal> GetDeletedMethods(IDefinition containingType)
            => GetDeletedMemberInternalSymbols(containingType, includeMethods: true, includeProperties: false, includeEvents: false);

        public ImmutableArray<ISymbolInternal> GetDeletedProperties(IDefinition containingType)
            => GetDeletedMemberInternalSymbols(containingType, includeMethods: false, includeProperties: true, includeEvents: false);

        public ImmutableArray<ISymbolInternal> GetDeletedEvents(IDefinition containingType)
            => GetDeletedMemberInternalSymbols(containingType, includeMethods: false, includeProperties: false, includeEvents: true);

        public bool IsReplaced(IDefinition definition, bool checkEnclosingTypes = false)
            => definition.GetInternalSymbol() is { } internalSymbol && IsReplaced(internalSymbol.GetISymbol(), checkEnclosingTypes);

        public bool IsReplaced(ISymbol symbol, bool checkEnclosingTypes = false)
        {
            ISymbol? currentSymbol = symbol;

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
        public bool RequiresCompilation(ISymbol symbol)
        {
            return this.GetChange(symbol) != SymbolChange.None;
        }

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
            var current = symbol.GetISymbol();
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
                return GetChange(symbol.GetISymbol());
            }

            // If the def that has no associated internal symbol existed in the previous generation, the def is unchanged
            // (although it may contain changed defs); otherwise, it was added.
            if (_definitionMap.DefinitionExists(def))
            {
                return (def is ITypeDefinition) ? SymbolChange.ContainsChanges : SymbolChange.None;
            }

            return SymbolChange.Added;
        }

        private SymbolChange GetChange(ISymbol symbol)
        {
            // In CalculateChanges we always store definitions for partial methods, so we have to
            // make sure we do the same thing here when we try to retrieve a change, as the compiler
            // associates synthesized methods with the implementation of the method that caused it
            // to be generated.
            if (symbol is IMethodSymbol method)
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
                    var internalSymbol = GetISymbolInternalOrNull(symbol);
                    if (internalSymbol is null)
                    {
                        return SymbolChange.None;
                    }

                    if (internalSymbol.Kind == SymbolKind.Namespace)
                    {
                        // If the namespace did not exist in the previous generation, it was added.
                        // Otherwise the namespace may contain changes.
                        return _definitionMap.NamespaceExists((INamespace)internalSymbol.GetCciAdapter()) ? SymbolChange.ContainsChanges : SymbolChange.Added;
                    }

                    // If the definition did not exist in the previous generation, it was added.
                    return DefinitionExistsInPreviousGeneration(internalSymbol) ? SymbolChange.None : SymbolChange.Added;

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
                    !IsReplaced(item.ContainingTypeDefinition, checkEnclosingTypes: true) &&
                    definitionExistsInAnyPreviousGeneration(item))
                {
                    return SymbolChange.Updated;
                }

                return change;
            }
        }

        protected abstract ISymbolInternal? GetISymbolInternalOrNull(ISymbol symbol);

        public IEnumerable<INamespaceTypeDefinition> GetTopLevelSourceTypeDefinitions(EmitContext context)
        {
            foreach (var symbol in _changes.Keys)
            {
                var namespaceTypeDef = (GetISymbolInternalOrNull(symbol)?.GetCciAdapter() as ITypeDefinition)?.AsNamespaceTypeDefinition(context);
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
        private static void CalculateChanges(IEnumerable<SemanticEdit> edits, out IReadOnlyDictionary<ISymbol, SymbolChange> changes, out ISet<ISymbol> replaceSymbols, out IReadOnlyDictionary<ISymbol, ISet<ISymbol>> deletedMembers)
        {
            var changesBuilder = new Dictionary<ISymbol, SymbolChange>();
            HashSet<ISymbol>? lazyReplaceSymbolsBuilder = null;
            Dictionary<ISymbol, ISet<ISymbol>>? lazyDeletedMembersBuilder = null;

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
                        Debug.Assert(edit.NewSymbol != null);
                        (lazyReplaceSymbolsBuilder ??= new HashSet<ISymbol>()).Add(edit.NewSymbol);
                        change = SymbolChange.Added;
                        break;

                    case SemanticEditKind.Delete:
                        // We allow method deletions only at the moment.
                        // For deletions NewSymbol is actually containing symbol
                        if (edit.OldSymbol is IMethodSymbol && edit.NewSymbol is { } newContainingSymbol)
                        {
                            Debug.Assert(edit.OldSymbol != null);
                            lazyDeletedMembersBuilder ??= new();
                            if (!lazyDeletedMembersBuilder.TryGetValue(newContainingSymbol, out var set))
                            {
                                set = new HashSet<ISymbol>();
                                lazyDeletedMembersBuilder.Add(newContainingSymbol, set);
                            }
                            set.Add(edit.OldSymbol);
                            // We need to make sure we track the containing type of the member being
                            // deleted, from the new compilation, in case the deletion is the only change.
                            if (!changesBuilder.ContainsKey(newContainingSymbol))
                            {
                                changesBuilder.Add(newContainingSymbol, SymbolChange.ContainsChanges);
                                AddContainingTypesAndNamespaces(changesBuilder, newContainingSymbol);
                            }
                        }
                        continue;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(edit.Kind);
                }

                var member = edit.NewSymbol;
                RoslynDebug.AssertNotNull(member);

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

                AddContainingTypesAndNamespaces(changesBuilder, member);

                // If we saw an edit for a symbol that is a nested type of this symbol, we might already have a dictionary entry
                // for it, flagging that it contains changes. If so we "upgrade" the change to the real one.
                if (changesBuilder.TryGetValue(member, out var existingChange) && existingChange == SymbolChange.ContainsChanges)
                {
                    Debug.Assert(member is INamedTypeSymbol);
                    changesBuilder[member] = change;
                }
                else
                {
                    changesBuilder.Add(member, change);
                }
            }

            changes = changesBuilder;
            replaceSymbols = lazyReplaceSymbolsBuilder ?? SpecializedCollections.EmptySet<ISymbol>();
            deletedMembers = lazyDeletedMembersBuilder ?? SpecializedCollections.EmptyReadOnlyDictionary<ISymbol, ISet<ISymbol>>();
        }

        private static void AddContainingTypesAndNamespaces(Dictionary<ISymbol, SymbolChange> changes, ISymbol symbol)
        {
            while (true)
            {
                var containingSymbol = GetContainingSymbol(symbol);
                if (containingSymbol == null || changes.ContainsKey(containingSymbol))
                {
                    return;
                }

                var change = containingSymbol.Kind is SymbolKind.Property or SymbolKind.Event ?
                    SymbolChange.Updated : SymbolChange.ContainsChanges;

                changes.Add(containingSymbol, change);
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
        private static ISymbol? GetContainingSymbol(ISymbol symbol)
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

        private static ISymbol? GetAssociatedSymbol(ISymbol symbol)
        {
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

            return null;
        }

        internal IDefinition? GetContainingDefinitionForBackingField(IFieldDefinition fieldDefinition)
        {
            var field = fieldDefinition.GetInternalSymbol()?.GetISymbol();
            if (field is null)
            {
                return null;
            }

            var associatedSymbol = GetAssociatedSymbol(field);
            if (associatedSymbol is not null)
            {
                return GetISymbolInternalOrNull(associatedSymbol)?.GetCciAdapter() as IDefinition;
            }

            return null;
        }
    }
}
