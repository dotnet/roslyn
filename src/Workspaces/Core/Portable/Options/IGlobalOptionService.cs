// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Provides services for reading and writing global client (in-proc) options
    /// shared across all workspaces.
    /// </summary>
    internal interface IGlobalOptionService
    {
        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(Option2<T> option);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(PerLanguageOption2<T> option, string? languageName);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        object? GetOption(OptionKey optionKey);

        /// <summary>
        /// Gets the current values of specified options.
        /// All options are read atomically.
        /// </summary>
        ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey> optionKeys);

        /// <summary>
        /// Applies a set of options.
        /// If any option changed its value invokes registered option persisters, updates current solutions of all registered workspaces and triggers <see cref="OptionChanged"/>.
        /// </summary>
        void SetOptions(OptionSet optionSet, IEnumerable<OptionKey> optionKeys);

        /// <summary>
        /// Sets and persists the value of a global option.
        /// Sets the value of a global option.
        /// Invokes registered option persisters.
        /// Triggers <see cref="OptionChanged"/>.
        /// Does not update any workspace (since this option is not a solution option).
        /// </summary>
        void SetGlobalOption(OptionKey optionKey, object? value);

        /// <summary>
        /// Atomically sets the values of specified global options. The option values are persisted.
        /// Triggers <see cref="OptionChanged"/>.
        /// Does not update any workspace (since this option is not a solution option).
        /// </summary>
        void SetGlobalOptions(ImmutableArray<OptionKey> optionKeys, ImmutableArray<object?> values);

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
