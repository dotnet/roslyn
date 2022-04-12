// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FixAll
{
    /// <summary>
    /// Language service for mapping spans for specific FixAllScopes for fix all occurences code fix.
    /// Every language that wants to support span based FixAll scopes, such as FixAllScope.ContainingMember,
    /// FixAllScope.ContainingType, should implement this language service. Non-span based FixAll scopes,
    /// such as FixAllScope.Document, FixAllScope.Project and FixAllScope.Solution
    /// do not require such a span mapping, and this service will never be called for these scopes. This language service
    /// does not need to be implemented by languages that only intend to support these non-span based FixAll scopes.
    /// </summary>
    internal interface IFixAllSpanMappingService : ILanguageService
    {
        /// <summary>
        /// For the given <paramref name="fixAllScope"/> and <paramref name="diagnosticSpan"/> in the given <paramref name="document"/>,
        /// returns the documents and fix all spans within each document that need to be fixed.
        /// Note that this API is only invoked for span based FixAll scopes, i.e. <see cref="CodeFixes.FixAllScope.ContainingMember"/>
        /// and <see cref="CodeFixes.FixAllScope.ContainingType"/>.
        /// </summary>
        Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(
            Document document, TextSpan diagnosticSpan, CodeFixes.FixAllScope fixAllScope, CancellationToken cancellationToken);

        /// <summary>
        /// For the given <paramref name="fixAllScope"/> and <paramref name="selectionSpan"/> in the given <paramref name="document"/>,
        /// returns the documents and fix all spans within each document that need to be fixed.
        /// Note that this API is only invoked for span based FixAll scopes, i.e. <see cref="CodeRefactorings.FixAllScope.ContainingMember"/>
        /// and <see cref="CodeRefactorings.FixAllScope.ContainingType"/>.
        /// </summary>
        Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(
            Document document, TextSpan selectionSpan, CodeRefactorings.FixAllScope fixAllScope, CancellationToken cancellationToken);
    }
}
