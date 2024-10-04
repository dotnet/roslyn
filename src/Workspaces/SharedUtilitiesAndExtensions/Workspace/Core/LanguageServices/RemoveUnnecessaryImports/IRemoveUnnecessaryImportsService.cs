// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

internal interface IRemoveUnnecessaryImportsService : ILanguageService
{
    Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken);

    Task<Document> RemoveUnnecessaryImportsAsync(Document fromDocument, Func<SyntaxNode, bool>? predicate, CancellationToken cancellationToken);
}
