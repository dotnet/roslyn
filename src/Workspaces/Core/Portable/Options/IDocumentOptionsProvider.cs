﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Implemented to provide options that apply to specific documents, like from .editorconfig files.
    /// </summary>
    /// <remarks>
    /// This is passed to <see cref="IOptionService.RegisterDocumentOptionsProvider(IDocumentOptionsProvider)"/> to activate it
    /// for a workspace. This instance then lives around for the lifetime of the workspace.
    /// </remarks>
    interface IDocumentOptionsProvider
    {
        /// <summary>
        /// Fetches a <see cref="IDocumentOptions"/> for the given document. Any asynchronous work (looking for config files, etc.)
        /// should be done here. Can return a null-valued task to mean there is no options being provided for this document.
        /// </summary>
        Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken);
    }
}
