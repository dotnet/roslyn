// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Implemented to provide options that apply to specific documents, like from .editorconfig files.
    /// </summary>
    /// <remarks>
    /// This is passed to <see cref="IOptionService.RegisterDocumentOptionsProvider(IDocumentOptionsProvider)"/> to activate it
    /// for a workspace. This instance then lives around for the lifetime of the workspace. This exists primarily
    /// because right now we're keeping this support only for the Visual Studio "15" workspace, so this offers an interface
    /// to meet in the middle.
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
