// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Provides services for reading and writing options.  This will provide support for
    /// customizations workspaces need to perform around options.  Note that 
    /// <see cref="IGlobalOptionService"/> options will normally still be offered through 
    /// implementations of this.  However, implementations may customize things differently 
    /// depending on their needs.
    /// </summary>
    internal interface IOptionService : IWorkspaceService
    {
        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T? GetOption<T>(Option<T> option);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T? GetOption<T>(Option2<T> option);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T? GetOption<T>(PerLanguageOption<T> option, string? languageName);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T? GetOption<T>(PerLanguageOption2<T> option, string? languageName);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        object? GetOption(OptionKey optionKey);

        /// <summary>
        /// Fetches an immutable set of all current options.
        /// </summary>
        SerializableOptionSet GetOptions();

        /// <summary>
        /// Gets a serializable option set snapshot with force computed values for all registered serializable options applicable for the given <paramref name="languages"/> by quering the option persisters.
        /// </summary>
        SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages);

        /// <summary>
        /// Applies a set of options.
        /// </summary>
        /// <param name="optionSet">New options to set.</param>
        void SetOptions(OptionSet optionSet);

        /// <summary>
        /// Returns the set of all registered options.
        /// </summary>
        IEnumerable<IOption> GetRegisteredOptions();

        /// <inheritdoc cref="IGlobalOptionService.TryMapEditorConfigKeyToOption"/>
        bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey);

        /// <summary>
        /// Returns the set of all registered serializable options applicable for the given <paramref name="languages"/>.
        /// </summary>
        ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages);

        event EventHandler<OptionChangedEventArgs> OptionChanged;

        /// <summary>
        /// Registers a provider that can modify the result of <see cref="Document.GetOptionsAsync(CancellationToken)"/>. Providers registered earlier are queried first
        /// for options, and the first provider to give a value wins.
        /// </summary>
        void RegisterDocumentOptionsProvider(IDocumentOptionsProvider documentOptionsProvider);

        /// <summary>
        /// Returns the <see cref="OptionSet"/> that applies to a specific document, given that document and the global options.
        /// </summary>
        Task<OptionSet> GetUpdatedOptionSetForDocumentAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken);

        /// <summary>
        /// Registers a workspace with the option service.
        /// </summary>
        void RegisterWorkspace(Workspace workspace);

        /// <summary>
        /// Unregisters a workspace from the option service.
        /// </summary>
        void UnregisterWorkspace(Workspace workspace);
    }
}
