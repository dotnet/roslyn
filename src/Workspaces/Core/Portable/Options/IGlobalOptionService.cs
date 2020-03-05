﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Provides services for reading and writing options.
    /// This will provide support for options at the global level (i.e. shared among
    /// all workspaces/services).
    /// 
    /// In general you should not import this type directly, and should instead get an
    /// <see cref="IOptionService"/> from <see cref="Workspace.Services"/>
    /// </summary>
    internal interface IGlobalOptionService
    {
        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        [return: MaybeNull]
        T GetOption<T>(Option<T> option);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        [return: MaybeNull]
        T GetOption<T>(PerLanguageOption<T> option, string? languageName);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        object? GetOption(OptionKey optionKey);

        /// <summary>
        /// Applies a set of options, invoking serializers if needed.
        /// </summary>
        void SetOptions(OptionSet optionSet);

        /// <summary>
        /// Gets force computed serializable options snapshot with prefetched values for the registered options applicable to the given <paramref name="languages"/> by quering the option persisters.
        /// </summary>
        SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages, IOptionService optionService);

        /// <summary>
        /// Returns the set of all registered options.
        /// </summary>
        IEnumerable<IOption> GetRegisteredOptions();

        /// <summary>
        /// Map an <strong>.editorconfig</strong> key to a corresponding <see cref="IEditorConfigStorageLocation2"/> and
        /// <see cref="OptionKey"/> that can be used to read and write the value stored in an <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="key">The <strong>.editorconfig</strong> key.</param>
        /// <param name="language">The language to use for the <paramref name="optionKey"/>, if the matching option has
        /// <see cref="IOption.IsPerLanguage"/> set.</param>
        /// <param name="storageLocation">The <see cref="IEditorConfigStorageLocation2"/> for the key.</param>
        /// <param name="optionKey">The <see cref="OptionKey"/> for the key and language.</param>
        /// <returns><see langword="true"/> if a matching option was found; otherwise, <see langword="false"/>.</returns>
        bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey);

        /// <summary>
        /// Returns the set of all registered serializable options applicable for the given <paramref name="languages"/>.
        /// </summary>
        ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages);

        event EventHandler<OptionChangedEventArgs>? OptionChanged;

        /// <summary>
        /// Refreshes the stored value of a serialized option. This should only be called from serializers.
        /// </summary>
        void RefreshOption(OptionKey optionKey, object? newValue);

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
