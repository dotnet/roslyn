// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigOptionMappingService : IWorkspaceService
    {
        public IEditorConfigOptionMapping Mapping { get; }
    }

    internal interface IEditorConfigOptionMapping
    {
        /// <summary>
        /// Map an <strong>.editorconfig</strong> key to a corresponding <see cref="OptionKey2"/>.
        /// </summary>
        bool TryMapEditorConfigKeyToOption(string key, [NotNullWhen(true)] out IOption2? option);
    }
}
