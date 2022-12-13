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
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class DocumentTaskListDiagnosticSource : IDiagnosticSource
{
    private static readonly ImmutableArray<string> s_todoCommentCustomTags = ImmutableArray.Create(PullDiagnosticConstants.TaskItemCustomTag);
    private static Tuple<ImmutableArray<string>, ImmutableArray<TaskListItemDescriptor>> s_lastRequestedTokens =
        Tuple.Create(ImmutableArray<string>.Empty, ImmutableArray<TaskListItemDescriptor>.Empty);

    private readonly Document _document;

    public DocumentTaskListSource(Document document)
        => _document = document;

    public ProjectOrDocumentId GetId() => new(_document.Id);
    public Project GetProject() => _document.Project;
    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(_document.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(_document.Project), Uri = _document.GetURI() }
            : null;

    public string ToDisplayString() => $"{_document.FilePath ?? _document.Name} in {_document.Project.Name}";

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken)
    {
        var service = _document.GetLanguageService<ITaskListService>();
        if (service == null)
            return ImmutableArray<DiagnosticData>.Empty;

        var tokenList = _document.Project.Solution.Options.GetOption(TaskListOptionsStorage.Descriptors);
        var descriptors = GetAndCacheDescriptors(tokenList);

        var items = await service.GetTaskListItemsAsync(_document, descriptors, cancellationToken).ConfigureAwait(false);
        if (items.Length == 0)
            return ImmutableArray<DiagnosticData>.Empty;

        return items.SelectAsArray(i => new DiagnosticData(
            id: "TODO",
            category: "TODO",
            message: i.Message,
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            warningLevel: 0,
            customTags: s_todoCommentCustomTags,
            properties: ImmutableDictionary<string, string?>.Empty,
            projectId: _document.Project.Id,
            language: _document.Project.Language,
            location: new DiagnosticDataLocation(i.Span, _document.Id, mappedFileSpan: i.MappedSpan)));
    }

    private static ImmutableArray<TaskListItemDescriptor> GetAndCacheDescriptors(ImmutableArray<string> tokenList)
    {
        var lastRequested = s_lastRequestedTokens;
        if (!lastRequested.Item1.SequenceEqual(tokenList))
        {
            var descriptors = TaskListItemDescriptor.Parse(tokenList);
            lastRequested = Tuple.Create(tokenList, descriptors);
            s_lastRequestedTokens = lastRequested;
        }

        return lastRequested.Item2;
    }
}
