// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueAnalyzer : ILanguageService
    {
        Task<DocumentAnalysisResults> AnalyzeDocumentAsync(Solution baseSolution, ImmutableArray<ActiveStatementSpan> activeStatements, Document document, CancellationToken cancellationToken);
        ImmutableArray<LinePositionSpan> GetExceptionRegions(SourceText text, SyntaxNode syntaxRoot, LinePositionSpan activeStatementSpan, bool isLeaf);
    }
}
