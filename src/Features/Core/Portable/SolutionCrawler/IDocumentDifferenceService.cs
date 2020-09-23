// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class DocumentDifferenceResult
    {
        public InvocationReasons ChangeType { get; }
        public SyntaxNode? ChangedMember { get; }

        public DocumentDifferenceResult(InvocationReasons changeType, SyntaxNode? changedMember = null)
        {
            ChangeType = changeType;
            ChangedMember = changedMember;
        }
    }

    internal interface IDocumentDifferenceService : ILanguageService
    {
        Task<DocumentDifferenceResult?> GetDifferenceAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken);
    }
}
