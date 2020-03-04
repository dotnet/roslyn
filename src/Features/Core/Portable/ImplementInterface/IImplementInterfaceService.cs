﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal interface IImplementInterfaceService : ILanguageService
    {
        Task<Document> ImplementInterfaceAsync(Document document, SyntaxNode node, CancellationToken cancellationToken);
        IEnumerable<CodeAction> GetCodeActions(Document document, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken);
    }
}
