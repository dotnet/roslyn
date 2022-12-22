// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation
    {
        bool TryParseValue(string value, out object? result);

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
}
