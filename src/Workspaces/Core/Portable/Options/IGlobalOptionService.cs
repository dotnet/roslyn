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
        T GetOption<T>(PerLanguageOption2<T> option, string language);

        /// <summary>
        /// Gets the current value of the specific option.
        /// </summary>
        T GetOption<T>(OptionKey2 optionKey);

        /// <summary>
        /// Gets the current values of specified options.
        /// All options are read atomically.
        /// </summary>
        ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey2> optionKeys);

        /// <summary>
        /// Sets values of options that may be stored in <see cref="Solution.Options"/> (public options).
        /// Clears <see cref="SolutionOptionSet"/> of registered workspaces so that next time
        /// <see cref="Solution.Options"/> are queried for the options new values are fetched from 
        /// <see cref="GlobalOptionService"/>.
        /// </summary>
        void SetOptions(ImmutableArray<KeyValuePair<OptionKey2, object?>> options);

        void SetGlobalOption<T>(Option2<T> option, T value);

        void SetGlobalOption<T>(PerLanguageOption2<T> option, string language, T value);

        /// <summary>
        /// Sets and persists the value of a global option.
        /// Sets the value of a global option.
        /// Invokes registered option persisters.
        /// Triggers <see cref="OptionChanged"/>.
        /// Does not update any workspace (since this option is not a solution option).
        /// </summary>
        void SetGlobalOption(OptionKey2 optionKey, object? value);

        /// <summary>
        /// Atomically sets the values of specified global options. The option values are persisted.
        /// Triggers <see cref="OptionChanged"/>.
        /// Does not update any workspace (since this option is not a solution option).
        /// </summary>
        void SetGlobalOptions(ImmutableArray<KeyValuePair<OptionKey2, object?>> options);

        event EventHandler<OptionChangedEventArgs>? OptionChanged;

        /// <summary>
        /// Refreshes the stored value of a serialized option. This should only be called from serializers.
        /// </summary>
        void RefreshOption(OptionKey2 optionKey, object? newValue);

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
