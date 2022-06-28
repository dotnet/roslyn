// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigOptionMappingService : IWorkspaceService
    {
        /// <summary>
        /// Map an <strong>.editorconfig</strong> key to a corresponding <see cref="IEditorConfigStorageLocation2"/> and
        /// <see cref="OptionKey"/> that can be used to read and write the value stored in an <see cref="OptionSet"/>.
        /// </summary>
        /// <param name="key">The <strong>.editorconfig</strong> key.</param>
        /// <param name="language">The language to use for the <paramref name="optionKey"/>, if the matching option has
        /// <see cref="IOption.IsPerLanguage"/> set.</param>
        /// <param name="storageLocation">The <see cref="IEditorConfigStorageLocation2"/> for the key.</param>
        /// <param name="optionKey">The <see cref="OptionKey"/> for the key and language.</param>
        /// <returns><see langword="true"/> if a matching option was found; otherwise, <see langword="false"/>.</returns>
        bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey);
    }
}
