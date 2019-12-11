// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Fetches an immutable set of all current options.
        /// </summary>
        WorkspaceOptionSet GetOptions();

        /// <summary>
        /// Applies a set of options.
        /// </summary>
        void SetOptions(OptionSet optionSet);

        /// <summary>
        /// Returns the set of all registered options.
        /// </summary>
        IEnumerable<IOption> GetRegisteredOptions();

        /// <summary>
        /// Returns the set of all registered serializable options.
        /// </summary>
        ImmutableHashSet<IOption> GetRegisteredSerializableOptions();

        /// <summary>
        /// Gets force computed option set for all the registered options by quering the option persisters.
        /// </summary>
        SerializableOptionSet GetSerializableOptions(IEnumerable<string> languages);

        event EventHandler<OptionChangedEventArgs> OptionChanged;
        event EventHandler<EventArgs> OptionsChanged;

        /// <summary>
        /// Registers a provider that can modify the result of <see cref="Document.GetOptionsAsync(CancellationToken)"/>. Providers registered earlier are queried first
        /// for options, and the first provider to give a value wins.
        /// </summary>
        void RegisterDocumentOptionsProvider(IDocumentOptionsProvider documentOptionsProvider);

        /// <summary>
        /// Returns the <see cref="OptionSet"/> that applies to a specific document, given that document and the global options.
        /// </summary>
        Task<OptionSet> GetUpdatedOptionSetForDocumentAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken);
    }
}
