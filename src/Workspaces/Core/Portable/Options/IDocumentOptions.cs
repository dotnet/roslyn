// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Returned from a <see cref="IDocumentOptionsProvider"/>
    /// </summary>
    internal interface IDocumentOptions
    {
        /// <summary>
        /// Attempts to fetch the value for the given option.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value">The value returned. May be null even if the function returns true as "null" may be valid value for some options.</param>
        /// <returns>True if this provider had a specific value for this option. False to indicate other providers should be queried.</returns>
        bool TryGetDocumentOption(OptionKey option, out object? value);
    }
}
