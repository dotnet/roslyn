// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Exportable by a host to specify the save and restore behavior for a particular set of
    /// values.
    /// </summary>
    internal interface IOptionPersister
    {
        bool TryFetch(OptionKey optionKey, out object? value);
        bool TryPersist(OptionKey optionKey, object? value);
    }
}
