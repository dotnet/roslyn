// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Returns the set of all registered options.
        /// </summary>
        IEnumerable<IOption> GetRegisteredOptions();

        /// <summary>
        /// Returns the set of all registered serializable options.
        /// </summary>
        ImmutableHashSet<IOption> GetRegisteredSerializableOptions();

        /// <summary>
        /// Gets force computed serializable options with prefetched values for all the registered options by quering the option persisters.
        /// </summary>
        ImmutableDictionary<OptionKey, object?> GetForceComputedRegisteredSerializableOptionValues(IEnumerable<string> languages);

        event EventHandler<OptionChangedEventArgs>? OptionChanged;

        event EventHandler<EventArgs>? OptionsChanged;

        /// <summary>
        /// Refreshes the stored value of a serialized option. This should only be called from serializers.
        /// </summary>
        void RefreshOption(OptionKey optionKey, object? newValue);
    }
}
