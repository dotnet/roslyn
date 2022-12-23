// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation
    {
        bool TryParseValue(string value, out object? result);

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
}
