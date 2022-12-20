// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options;

internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
{
    /// <summary>
    /// The name of the editorconfig key for the option.
    /// </summary>
    string KeyName { get; }

    /// <summary>
    /// Gets the editorconfig string representation for the specified <paramref name="value"/>. 
    /// </summary>
    string GetEditorConfigStringValue(object? value);

#if !CODE_STYLE
    /// <summary>
    /// Gets the editorconfig string representation for the option value stored in <paramref name="optionSet"/>.
    /// May combine values of multiple options stored in the set.
    /// </summary>
    string GetEditorConfigStringValue(OptionKey optionKey, OptionSet optionSet);
#endif
}

internal static partial class Extensions
{
    /// <summary>
    /// Gets the editorconfig string representation for this storage location. The result is a complete line of the
    /// <strong>.editorconfig</strong> file, such as the following:
    /// <code>
    /// dotnet_sort_system_directives_first = true
    /// </code>
    /// </summary>
    public static string GetEditorConfigString(this IEditorConfigStorageLocation2 location, object? value)
        => $"{location.KeyName} = {location.GetEditorConfigStringValue(value)}";
}
