// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServices.AdditionalProperty;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        /// <summary>
        /// Context to be used for FindAllReferences (as opposed to FindImplementations/GoToDef).
        /// This context supports showing reference items, and will display appropriate messages
        /// about no-references being found for a definition at the end of the search.
        /// </summary>
        private class WithReferencesFindUsagesContext : AbstractTableDataSourceFindUsagesContext
        {
            public WithReferencesFindUsagesContext(
                StreamingFindUsagesPresenter presenter,
                IFindAllReferencesWindow findReferencesWindow,
                ImmutableArray<AbstractAdditionalPropertyDefinition> customColumns)
                : base(presenter, findReferencesWindow, customColumns)
            {
            }

            protected override async Task OnDefinitionFoundWorkerAsync(DefinitionItem definition)
            {
                // If this is a definition we always want to show, then create entries
                // for all the declaration locations immediately.  Otherwise, we'll 
                // create them on demand when we hear about references for this definition.
                if (definition.DisplayIfNoReferences)
                {
                    await AddDeclarationEntriesAsync(definition).ConfigureAwait(false);
                }
            }

            private async Task AddDeclarationEntriesAsync(DefinitionItem definition)
            {
                CancellationToken.ThrowIfCancellationRequested();

                // Don't do anything if we already have declaration entries for this definition 
                // (i.e. another thread beat us to this).
                if (HasDeclarationEntries(definition))
                {
                    return;
                }

                var definitionBucket = GetOrCreateDefinitionBucket(definition);

                // We could do this inside the lock.  but that would mean async activity in a 
                // lock, and I'd like to avoid that.  That does mean that we might do extra
                // work if multiple threads end up down this path.  But only one of them will
                // win when we access the lock below.
                var declarations = ArrayBuilder<Entry>.GetInstance();
                foreach (var declarationLocation in definition.SourceSpans)
                {
                    var definitionEntry = await TryCreateDocumentSpanEntryAsync(
                        definitionBucket, declarationLocation, HighlightSpanKind.Definition, referenceUsageInfo: null, additionalProperties: definition.DisplayableProperties).ConfigureAwait(false);
                    declarations.AddIfNotNull(definitionEntry);
                }

                var changed = false;
                lock (Gate)
                {
                    // Do one final check to ensure that no other thread beat us here.
                    if (!HasDeclarationEntries(definition))
                    {
                        // We only include declaration entries in the entries we show when 
                        // not grouping by definition.
                        EntriesWhenNotGroupingByDefinition = EntriesWhenNotGroupingByDefinition.AddRange(declarations);
                        CurrentVersionNumber++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    // Let all our subscriptions know that we've updated.
                    NotifyChange();
                }

                declarations.Free();
            }

            private bool HasDeclarationEntries(DefinitionItem definition)
            {
                lock (Gate)
                {
                    return EntriesWhenNotGroupingByDefinition.Any(
                        e => e.DefinitionBucket.DefinitionItem == definition);
                }
            }

            protected override Task OnReferenceFoundWorkerAsync(SourceReferenceItem reference)
            {
                // Normal references go into both sets of entries.
                return OnEntryFoundAsync(
                    reference.Definition,
                    bucket => TryCreateDocumentSpanEntryAsync(
                        bucket, reference.SourceSpan,
                        reference.IsWrittenTo ? HighlightSpanKind.WrittenReference : HighlightSpanKind.Reference,
                        reference.AdditionalPropertiesWithMultipleValues,
                        reference.AdditionalProperties),
                    addToEntriesWhenGroupingByDefinition: true,
                    addToEntriesWhenNotGroupingByDefinition: true);
            }

            protected async Task OnEntryFoundAsync(
                DefinitionItem definition,
                Func<RoslynDefinitionBucket, Task<Entry>> createEntryAsync,
                bool addToEntriesWhenGroupingByDefinition,
                bool addToEntriesWhenNotGroupingByDefinition)
            {
                Debug.Assert(addToEntriesWhenGroupingByDefinition || addToEntriesWhenNotGroupingByDefinition);
                CancellationToken.ThrowIfCancellationRequested();

                // OK, we got a *reference* to some definition item.  This may have been
                // a reference for some definition that we haven't created any declaration
                // entries for (i.e. because it had DisplayIfNoReferences = false).  Because
                // we've now found a reference, we want to make sure all its declaration
                // entries are added.
                await AddDeclarationEntriesAsync(definition).ConfigureAwait(false);

                // First find the bucket corresponding to our definition.
                var definitionBucket = GetOrCreateDefinitionBucket(definition);
                var entry = await createEntryAsync(definitionBucket).ConfigureAwait(false);
                if (entry == null)
                {
                    return;
                }

                lock (Gate)
                {
                    // Once we can make the new entry, add it to the appropriate list.
                    if (addToEntriesWhenGroupingByDefinition)
                    {
                        EntriesWhenGroupingByDefinition = EntriesWhenGroupingByDefinition.Add(entry);
                    }

                    if (addToEntriesWhenNotGroupingByDefinition)
                    {
                        EntriesWhenNotGroupingByDefinition = EntriesWhenNotGroupingByDefinition.Add(entry);
                    }

                    CurrentVersionNumber++;
                }

                // Let all our subscriptions know that we've updated.
                NotifyChange();
            }

            protected override async Task OnCompletedAsyncWorkerAsync()
            {
                // Now that we know the search is over, create and display any error messages
                // for definitions that were not found.
                await CreateMissingReferenceEntriesIfNecessaryAsync().ConfigureAwait(false);
                await CreateNoResultsFoundEntryIfNecessaryAsync().ConfigureAwait(false);
            }

            private async Task CreateMissingReferenceEntriesIfNecessaryAsync()
            {
                await CreateMissingReferenceEntriesIfNecessaryAsync(whenGroupingByDefinition: true).ConfigureAwait(false);
                await CreateMissingReferenceEntriesIfNecessaryAsync(whenGroupingByDefinition: false).ConfigureAwait(false);
            }

            private async Task CreateMissingReferenceEntriesIfNecessaryAsync(
                bool whenGroupingByDefinition)
            {
                // Go through and add dummy entries for any definitions that 
                // that we didn't find any references for.

                var definitions = GetDefinitionsToCreateMissingReferenceItemsFor(whenGroupingByDefinition);
                foreach (var definition in definitions)
                {
                    // Create a fake reference to this definition that says 
                    // "no references found to <symbolname>".
                    await OnEntryFoundAsync(definition,
                        bucket => SimpleMessageEntry.CreateAsync(
                            bucket, GetMessage(bucket.DefinitionItem)),
                        addToEntriesWhenGroupingByDefinition: whenGroupingByDefinition,
                        addToEntriesWhenNotGroupingByDefinition: !whenGroupingByDefinition).ConfigureAwait(false);
                }
            }

            private static string GetMessage(DefinitionItem definition)
            {
                if (definition.IsExternal)
                {
                    return ServicesVSResources.External_reference_found;
                }

                return string.Format(
                    ServicesVSResources.No_references_found_to_0,
                    definition.NameDisplayParts.JoinText());
            }

            private ImmutableArray<DefinitionItem> GetDefinitionsToCreateMissingReferenceItemsFor(
                bool whenGroupingByDefinition)
            {
                lock (Gate)
                {
                    var entries = whenGroupingByDefinition
                        ? EntriesWhenGroupingByDefinition
                        : EntriesWhenNotGroupingByDefinition;

                    // Find any definitions that we didn't have any references to. But only show 
                    // them if they want to be displayed without any references.  This will 
                    // ensure that we still see things like overrides and whatnot, but we
                    // won't show property-accessors.
                    var seenDefinitions = entries.Select(r => r.DefinitionBucket.DefinitionItem).ToSet();
                    var q = from definition in Definitions
                            where !seenDefinitions.Contains(definition) &&
                                  definition.DisplayIfNoReferences
                            select definition;

                    // If we find at least one of these types of definitions, then just return those.
                    var result = ImmutableArray.CreateRange(q);
                    if (result.Length > 0)
                    {
                        return result;
                    }

                    // We found no definitions that *want* to be displayed.  However, we still 
                    // want to show something.  So, if necessary, show at lest the first definition
                    // even if we found no references and even if it would prefer to not be seen.
                    if (entries.Count == 0 && Definitions.Count > 0)
                    {
                        return ImmutableArray.Create(Definitions.First());
                    }

                    return ImmutableArray<DefinitionItem>.Empty;
                }
            }

            private async Task CreateNoResultsFoundEntryIfNecessaryAsync()
            {
                bool noDefinitions;
                lock (Gate)
                {
                    noDefinitions = this.Definitions.Count == 0;
                }

                if (noDefinitions)
                {
                    // Create a fake definition/reference called "search found no results"
                    await OnEntryFoundAsync(NoResultsDefinitionItem,
                        bucket => SimpleMessageEntry.CreateAsync(
                            bucket, ServicesVSResources.Search_found_no_results),
                        addToEntriesWhenGroupingByDefinition: true,
                        addToEntriesWhenNotGroupingByDefinition: true).ConfigureAwait(false);
                }
            }

            private static readonly DefinitionItem NoResultsDefinitionItem =
                DefinitionItem.CreateNonNavigableItem(
                    GlyphTags.GetTags(Glyph.StatusInformation),
                    ImmutableArray.Create(new TaggedText(
                        TextTags.Text,
                        ServicesVSResources.Search_found_no_results)));
        }
    }
}
