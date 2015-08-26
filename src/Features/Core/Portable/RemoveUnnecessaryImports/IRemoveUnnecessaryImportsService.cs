// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal interface IRemoveUnnecessaryImportsService : ILanguageService
    {
        /// <returns>Returns the rewritten document, or the document passed in if no changes were made. If cancellation
        /// was observed, it returns null.</returns>
        Document RemoveUnnecessaryImports(Document document, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken);
    }
}
