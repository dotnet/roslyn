// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
#if false
    internal class DocumentDifferenceResult(InvocationReasons changeType, SyntaxNode? changedMember = null)
    {
        public InvocationReasons ChangeType { get; } = changeType;
        public SyntaxNode? ChangedMember { get; } = changedMember;
    }
#endif

    internal interface IDocumentDifferenceService : ILanguageService
    {
#if false
        Task<DocumentDifferenceResult?> GetDifferenceAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken);
#endif
        Task<SyntaxNode?> GetChangedMemberAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken);
    }
}
