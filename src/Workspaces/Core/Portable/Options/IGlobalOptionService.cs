// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Provides services for reading and writing global client (in-proc) options
/// shared across all workspaces.
/// </summary>
internal interface IGlobalOptionService : IOptionsReader
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

    void SetGlobalOption<T>(Option2<T> option, T value);

    void SetGlobalOption<T>(PerLanguageOption2<T> option, string language, T value);

    /// <summary>
    /// Sets and persists the value of a global option.
    /// Sets the value of a global option.
    /// Invokes registered option persisters.
    /// Triggers option changed event for handlers registered with <see cref="AddOptionChangedHandler"/>.
    /// </summary>
    void SetGlobalOption(OptionKey2 optionKey, object? value);

    /// <summary>
    /// Atomically sets the values of specified global options. The option values are persisted.
    /// Triggers option changed event for handlers registered with <see cref="AddOptionChangedHandler"/>.
    /// </summary>
    /// <remarks>
    /// Returns true if any option changed its value stored in the global options.
    /// </remarks>
    bool SetGlobalOptions(ImmutableArray<KeyValuePair<OptionKey2, object?>> options);

    /// <summary>
    /// Refreshes the stored value of an option. This should only be called from persisters.
    /// Does not persist the new option value.
    /// </summary>
    /// <remarks>
    /// Returns true if the option changed its value stored in the global options.
    /// </remarks>
    bool RefreshOption(OptionKey2 optionKey, object? newValue);

    void AddOptionChangedHandler(object target, WeakEventHandler<OptionChangedEventArgs> handler);

    void RemoveOptionChangedHandler(object target, WeakEventHandler<OptionChangedEventArgs> handler);
}
