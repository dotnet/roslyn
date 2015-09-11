// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal static class RemoveUnnecessaryImportsService
    {
        public static async Task<Document> RemoveUnnecessaryImportsAsync(Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            document = document.GetLanguageService<IRemoveUnnecessaryImportsService>().RemoveUnnecessaryImports(
                document,
                await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false),
                await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken);

            // The underlying service returns null if cancellation happened. For a public API, we should be throwing.
            cancellationToken.ThrowIfCancellationRequested();

            return document;
        }
    }
}
