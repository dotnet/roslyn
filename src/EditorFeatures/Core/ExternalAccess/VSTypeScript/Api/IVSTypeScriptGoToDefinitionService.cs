﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    [Obsolete("TS, remove your implementation of this type now that you're entirely on LSP for go-to-def.  Then let us know.", error: false)]
    internal interface IVSTypeScriptGoToDefinitionService
    {
        Task<IEnumerable<IVSTypeScriptNavigableItem>?> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);
        bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken);
    }
}
