﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractDocumentDiagnosticSource<TDocument> : IDiagnosticSource
    where TDocument : TextDocument
{
    private static readonly ImmutableArray<string> s_todoCommentCustomTags = ImmutableArray.Create(PullDiagnosticConstants.TaskItemCustomTag);

    private static Tuple<ImmutableArray<string>, ImmutableArray<TodoCommentDescriptor>> s_lastRequestedTokens =
        Tuple.Create(ImmutableArray<string>.Empty, ImmutableArray<TodoCommentDescriptor>.Empty);

    protected readonly TDocument Document;

    protected AbstractDocumentDiagnosticSource(TDocument document)
    {
        this.Document = document;
    }

    public ProjectOrDocumentId GetId() => new(Document.Id);
    public Project GetProject() => Document.Project;
    public Uri GetUri() => Document.GetURI();

    protected abstract bool IncludeTodoComments { get; }
    protected abstract bool IncludeStandardDiagnostics { get; }

    protected abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsWorkerAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken);

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
    {
        var todoComments = IncludeTodoComments ? await this.GetTodoCommentDiagnosticsAsync(cancellationToken).ConfigureAwait(false) : ImmutableArray<DiagnosticData>.Empty;
        var diagnostics = IncludeStandardDiagnostics ? await this.GetDiagnosticsWorkerAsync(diagnosticAnalyzerService, context, diagnosticMode, cancellationToken).ConfigureAwait(false) : ImmutableArray<DiagnosticData>.Empty;
        return todoComments.AddRange(diagnostics);
    }

    private async Task<ImmutableArray<DiagnosticData>> GetTodoCommentDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (this.Document is not Document document)
            return ImmutableArray<DiagnosticData>.Empty;

        var service = document.GetLanguageService<ITodoCommentDataService>();
        if (service == null)
            return ImmutableArray<DiagnosticData>.Empty;

        var tokenList = document.Project.Solution.Options.GetOption(TodoCommentOptionsStorage.TokenList);
        var descriptors = GetAndCacheDescriptors(tokenList);

        var comments = await service.GetTodoCommentDataAsync(document, descriptors, cancellationToken).ConfigureAwait(false);
        return comments.SelectAsArray(comment => new DiagnosticData(
            id: "TODO",
            category: "TODO",
            message: comment.Message,
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            warningLevel: 0,
            customTags: s_todoCommentCustomTags,
            properties: ImmutableDictionary<string, string?>.Empty,
            projectId: document.Project.Id,
            language: document.Project.Language,
            location: new DiagnosticDataLocation(
                document.Id,
                originalFilePath: comment.Span.Path,
                mappedFilePath: comment.MappedSpan.Path,
                originalStartLine: comment.Span.StartLinePosition.Line,
                originalStartColumn: comment.Span.StartLinePosition.Character,
                originalEndLine: comment.Span.EndLinePosition.Line,
                originalEndColumn: comment.Span.EndLinePosition.Character,
                mappedStartLine: comment.MappedSpan.StartLinePosition.Line,
                mappedStartColumn: comment.MappedSpan.StartLinePosition.Character,
                mappedEndLine: comment.MappedSpan.EndLinePosition.Line,
                mappedEndColumn: comment.MappedSpan.EndLinePosition.Character)));
    }

    private static ImmutableArray<TodoCommentDescriptor> GetAndCacheDescriptors(ImmutableArray<string> tokenList)
    {
        var lastRequested = s_lastRequestedTokens;
        if (!lastRequested.Item1.SequenceEqual(tokenList))
        {
            var descriptors = TodoCommentDescriptor.Parse(tokenList);
            lastRequested = Tuple.Create(tokenList, descriptors);
            s_lastRequestedTokens = lastRequested;
        }

        return lastRequested.Item2;
    }
}
