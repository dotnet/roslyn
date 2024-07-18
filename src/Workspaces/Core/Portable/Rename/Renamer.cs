// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

public static partial class Renamer
{
    [Obsolete]
    private static SymbolRenameOptions GetSymbolRenameOptions(OptionSet optionSet)
        => new(
            RenameOverloads: optionSet.GetOption(RenameOptions.RenameOverloads),
            RenameInStrings: optionSet.GetOption(RenameOptions.RenameInStrings),
            RenameInComments: optionSet.GetOption(RenameOptions.RenameInComments),
            RenameFile: false);

    [Obsolete]
    private static DocumentRenameOptions GetDocumentRenameOptions(OptionSet optionSet)
        => new(
            RenameMatchingTypeInStrings: optionSet.GetOption(RenameOptions.RenameInStrings),
            RenameMatchingTypeInComments: optionSet.GetOption(RenameOptions.RenameInComments));

    [Obsolete("Use overload taking RenameOptions")]
    public static Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, OptionSet? optionSet, CancellationToken cancellationToken = default)
        => RenameSymbolAsync(solution, symbol, GetSymbolRenameOptions(optionSet ?? solution.Options), newName, cancellationToken);

    public static async Task<Solution> RenameSymbolAsync(
        Solution solution, ISymbol symbol, SymbolRenameOptions options, string newName, CancellationToken cancellationToken = default)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        if (string.IsNullOrEmpty(newName))
            throw new ArgumentException(WorkspacesResources._0_must_be_a_non_null_and_non_empty_string, nameof(newName));

        var resolution = await RenameSymbolAsync(solution, symbol, newName, options, nonConflictSymbolKeys: default, cancellationToken).ConfigureAwait(false);

        if (resolution.IsSuccessful)
        {
            return resolution.NewSolution;
        }
        else
        {
            // This is a public entry-point.  So if rename failed to resolve conflicts, we report that back to caller as
            // an exception.
            throw new ArgumentException(resolution.ErrorMessage);
        }
    }

    [Obsolete("Use overload taking RenameOptions")]
    public static Task<RenameDocumentActionSet> RenameDocumentAsync(
        Document document,
        string? newDocumentName,
        IReadOnlyList<string>? newDocumentFolders = null,
        OptionSet? optionSet = null,
        CancellationToken cancellationToken = default)
        => RenameDocumentAsync(document, GetDocumentRenameOptions(optionSet ?? document.Project.Solution.Options), newDocumentName, newDocumentFolders, cancellationToken);

    /// <summary>
    /// Call to perform a rename of document or change in document folders. Returns additional code changes related to the document
    /// being modified, such as renaming symbols in the file. 
    ///
    /// Each change is added as a <see cref="RenameDocumentAction"/> in the returned <see cref="RenameDocumentActionSet.ApplicableActions" />.
    /// 
    /// Each action may individually encounter errors that prevent it from behaving correctly. Those are reported in <see cref="RenameDocumentAction.GetErrors(System.Globalization.CultureInfo?)"/>.
    /// 
    /// <para />
    /// 
    /// Current supported actions that may be returned: 
    /// <list>
    ///  <item>Rename symbol action that will rename the type to match the document name.</item>
    ///  <item>Sync namespace action that will sync the namespace(s) of the document to match the document folders. </item>
    /// </list>
    /// 
    /// </summary>
    /// <param name="document">The document to be modified</param>
    /// <param name="newDocumentName">The new name for the document. Pass null or the same name to keep unchanged.</param>
    /// <param name="options">Options used to configure rename of a type contained in the document that matches the document's name.</param>
    /// <param name="newDocumentFolders">The new set of folders for the <see cref="TextDocument.Folders"/> property</param>
    public static async Task<RenameDocumentActionSet> RenameDocumentAsync(
        Document document,
        DocumentRenameOptions options,
        string? newDocumentName,
        IReadOnlyList<string>? newDocumentFolders = null,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (document.Services.GetService<ISpanMappingService>() != null)
        {
            // Don't advertise that we can file rename generated documents that map to a different file.
            return new RenameDocumentActionSet([], document.Id, document.Name, [.. document.Folders], options);
        }

        using var _ = ArrayBuilder<RenameDocumentAction>.GetInstance(out var actions);

        if (newDocumentName != null && !newDocumentName.Equals(document.Name))
        {
            var renameAction = await RenameSymbolDocumentAction.TryCreateAsync(document, newDocumentName, cancellationToken).ConfigureAwait(false);
            actions.AddIfNotNull(renameAction);
        }

        if (newDocumentFolders != null && !newDocumentFolders.SequenceEqual(document.Folders))
        {
            var action = SyncNamespaceDocumentAction.TryCreate(document, newDocumentFolders);
            actions.AddIfNotNull(action);
        }

        newDocumentName ??= document.Name;
        newDocumentFolders ??= document.Folders;

        return new RenameDocumentActionSet(
            actions.ToImmutable(),
            document.Id,
            newDocumentName,
            [.. newDocumentFolders],
            options);
    }

    /// <inheritdoc cref="LightweightRenameLocations.FindRenameLocationsAsync"/>
    internal static Task<LightweightRenameLocations> FindRenameLocationsAsync(Solution solution, ISymbol symbol, SymbolRenameOptions options, CancellationToken cancellationToken)
        => LightweightRenameLocations.FindRenameLocationsAsync(symbol, solution, options, cancellationToken);

    internal static async Task<ConflictResolution> RenameSymbolAsync(
        Solution solution,
        ISymbol symbol,
        string newName,
        SymbolRenameOptions options,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(solution);
        Contract.ThrowIfNull(symbol);
        Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

        cancellationToken.ThrowIfCancellationRequested();

        using (Logger.LogBlock(FunctionId.Renamer_RenameSymbolAsync, cancellationToken))
        {
            if (SerializableSymbolAndProjectId.TryCreate(symbol, solution, cancellationToken, out var serializedSymbol))
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.TryInvokeAsync<IRemoteRenamerService, SerializableConflictResolution?>(
                        solution,
                        (service, solutionInfo, cancellationToken) => service.RenameSymbolAsync(
                            solutionInfo,
                            serializedSymbol,
                            newName,
                            options,
                            nonConflictSymbolKeys,
                            cancellationToken),
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue && result.Value != null)
                        return await result.Value.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

                    // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
                }
            }
        }

        return await RenameSymbolInCurrentProcessAsync(
            solution, symbol, newName, options,
            nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ConflictResolution> RenameSymbolInCurrentProcessAsync(
        Solution solution,
        ISymbol symbol,
        string newName,
        SymbolRenameOptions options,
        ImmutableArray<SymbolKey> nonConflictSymbolKeys,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(solution);
        Contract.ThrowIfNull(symbol);
        Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

        cancellationToken.ThrowIfCancellationRequested();

        // Since we know we're in the oop process, we know we won't need to make more OOP calls.  Since this is the
        // rename entry-point that does the entire rename, we can directly use the heavyweight RenameLocations type,
        // without having to go through any intermediary LightweightTypes.
        var renameLocations = await SymbolicRenameLocations.FindLocationsInCurrentProcessAsync(symbol, solution, options, cancellationToken).ConfigureAwait(false);
        return await ConflictResolver.ResolveSymbolicLocationConflictsInCurrentProcessAsync(
            renameLocations, newName, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
    }
}
