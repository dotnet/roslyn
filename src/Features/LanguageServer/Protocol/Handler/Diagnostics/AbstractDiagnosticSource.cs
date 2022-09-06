// Licensed to the .NET Foundation under one or more agreements.
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

internal abstract record class AbstractDiagnosticSource : IDiagnosticSource
{
    private static readonly ImmutableArray<string> s_todoCommentCustomTags = ImmutableArray.Create(PullDiagnosticConstants.TaskItemCustomTag);

    private static Tuple<ImmutableArray<string>, ImmutableArray<TodoCommentDescriptor>> s_lastRequestedTokens =
        Tuple.Create(ImmutableArray<string>.Empty, ImmutableArray<TodoCommentDescriptor>.Empty);

    protected AbstractDiagnosticSource()
    {
    }

    public abstract Project GetProject();
    public abstract ProjectOrDocumentId GetId();
    public abstract Uri GetUri();
    public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken);

    protected async Task<ImmutableArray<DiagnosticData>> GetTodoCommentDiagnostics(Document document, CancellationToken cancellationToken)
    {
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
                originalFilePath: comment.OriginalFilePath,
                mappedFilePath: comment.MappedFilePath,
                originalStartLine: comment.OriginalLine,
                originalStartColumn: comment.OriginalColumn,
                originalEndLine: comment.OriginalLine,
                originalEndColumn: comment.OriginalColumn,
                mappedStartLine: comment.MappedLine,
                mappedStartColumn: comment.MappedColumn,
                mappedEndLine: comment.MappedLine,
                mappedEndColumn: comment.MappedColumn)));
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
