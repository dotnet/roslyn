// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

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
