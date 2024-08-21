// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal partial class StreamingFindUsagesPresenter
{
    /// <summary>
    /// Context to be used for FindImplementations/GoToDef (as opposed to FindReferences).
    /// This context will not group entries by definition, and will instead just create
    /// entries for the definitions themselves.
    /// </summary>
    private sealed class WithoutReferencesFindUsagesContext(
        StreamingFindUsagesPresenter presenter,
        IFindAllReferencesWindow findReferencesWindow,
        ImmutableArray<ITableColumnDefinition> customColumns,
        IGlobalOptionService globalOptions,
        bool includeContainingTypeAndMemberColumns,
        bool includeKindColumn,
        IThreadingContext threadingContext)
        : AbstractTableDataSourceFindUsagesContext(
            presenter,
            findReferencesWindow,
            customColumns,
            globalOptions,
            includeContainingTypeAndMemberColumns,
            includeKindColumn,
            threadingContext)
    {

        // We should never be called in a context where we get references.
        protected override ValueTask OnReferenceFoundWorkerAsync(SourceReferenceItem reference, CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        // Nothing to do on completion.
        protected override Task OnCompletedAsyncWorkerAsync(CancellationToken cancellationToken)
            => CreateNoResultsFoundEntryIfNecessaryAsync();

        private async Task CreateNoResultsFoundEntryIfNecessaryAsync()
        {
            string message;
            lock (Gate)
            {
                // If we got definitions, then no need to show the 'no results found' message.
                if (this.Definitions.Count > 0)
                    return;

                message = NoDefinitionsFoundMessage;
            }

            var definitionBucket = GetOrCreateDefinitionBucket(CreateNoResultsDefinitionItem(message), expandedByDefault: true);
            var entry = await SimpleMessageEntry.CreateAsync(definitionBucket, navigationBucket: null, message).ConfigureAwait(false);

            var isPrimary = IsPrimary(definitionBucket.DefinitionItem);

            lock (Gate)
            {
                Add(EntriesWhenGroupingByDefinition, entry, isPrimary);
                Add(EntriesWhenNotGroupingByDefinition, entry, isPrimary);

                CurrentVersionNumber++;
            }

            NotifyChange();
        }

        protected override async ValueTask OnDefinitionFoundWorkerAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            var definitionBucket = GetOrCreateDefinitionBucket(definition, expandedByDefault: true);

            using var _ = ArrayBuilder<Entry>.GetInstance(out var entries);

            if (definition.SourceSpans.IsEmpty && definition.MetadataLocations.IsEmpty)
            {
                entries.Add(new NonNavigableDefinitionItemEntry(this, definitionBucket));
            }
            else if (definition.SourceSpans.Length == 1 && definition.MetadataLocations.IsEmpty)
            {
                // If we only have a single location, then use the DisplayParts of the
                // definition as what to show.  That way we show enough information for things
                // methods.  i.e. we'll show "void TypeName.MethodName(args...)" allowing
                // the user to see the type the method was created in.
                var entry = await TryCreateDefinitionEntryAsync(definitionBucket, definition, cancellationToken).ConfigureAwait(false);
                entries.AddIfNotNull(entry);
            }
            else
            {
                // If we have multiple spans (i.e. for partial types), then create a 
                // DocumentSpanEntry for each.  That way we can easily see the source
                // code where each location is to help the user decide which they want
                // to navigate to.
                await AddDocumentSpanEntriesAsync(entries, definitionBucket, definition, cancellationToken).ConfigureAwait(false);

                foreach (var metadataLocation in definition.MetadataLocations)
                {
                    entries.Add(new MetadataDefinitionItemEntry(this, definitionBucket, metadataLocation, ThreadingContext));
                }
            }

            if (entries.Count > 0)
            {
                var isPrimary = IsPrimary(definition);

                lock (Gate)
                {
                    AddRange(EntriesWhenGroupingByDefinition, entries, isPrimary);
                    AddRange(EntriesWhenNotGroupingByDefinition, entries, isPrimary);
                    CurrentVersionNumber++;
                }

                NotifyChange();
            }
        }
    }
}
