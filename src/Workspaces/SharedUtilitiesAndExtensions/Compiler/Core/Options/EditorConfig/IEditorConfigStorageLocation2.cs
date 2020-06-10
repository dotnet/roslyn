// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
    {
        string KeyName { get; }

        /// <summary>
        /// Gets the editorconfig string representation for this storage location. The result is a complete line of the
        /// <strong>.editorconfig</strong> file, such as the following:
        /// <code>
        /// dotnet_sort_system_directives_first = true
        /// </code>
        /// </summary>
        string GetEditorConfigString(object? value, OptionSet optionSet);

        /// <summary>
        /// Gets the editorconfig string representation for this storage location. The result only includes the value
        /// for the <strong>.editorconfig</strong> entry.
        /// </summary>
        /// <seealso cref="GetEditorConfigString"/>
        string GetEditorConfigStringValue(object? value, OptionSet optionSet);
    }
}
