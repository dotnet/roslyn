// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueAnalyzer : ILanguageService
    {
        Task<DocumentAnalysisResults> AnalyzeDocumentAsync(
            Project baseProject,
            AsyncLazy<ActiveStatementsMap> lazyBaseActiveStatements,
            Document document,
            ImmutableArray<LinePositionSpan> newActiveStatementSpans,
            AsyncLazy<EditAndContinueCapabilities> lazyCapabilities,
            CancellationToken cancellationToken);

        ActiveStatementExceptionRegions GetExceptionRegions(SyntaxNode syntaxRoot, TextSpan unmappedActiveStatementSpan, bool isNonLeaf, CancellationToken cancellationToken);
    }
}
