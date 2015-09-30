// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal partial class AbstractSuppressionCodeFixProvider
    {
        /// <summary>
        /// Suppression code action based on pragma add/remove/edit.
        /// </summary>
        internal interface IPragmaBasedCodeAction
        {
            Task<Document> GetChangedDocumentAsync(bool includeStartTokenChange, bool includeEndTokenChange, CancellationToken cancellationToken);
        }
    }
}
