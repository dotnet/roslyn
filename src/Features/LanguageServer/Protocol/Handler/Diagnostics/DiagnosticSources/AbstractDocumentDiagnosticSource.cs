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

internal abstract class AbstractDocumentDiagnosticSource<TDocument> : IDiagnosticSource
    where TDocument : TextDocument
{
    private static readonly ImmutableArray<string> s_todoCommentCustomTags = ImmutableArray.Create(PullDiagnosticConstants.TaskItemCustomTag);

    private static Tuple<ImmutableArray<string>, ImmutableArray<TaskListItemDescriptor>> s_lastRequestedTokens =
        Tuple.Create(ImmutableArray<string>.Empty, ImmutableArray<TaskListItemDescriptor>.Empty);

    protected readonly TDocument Document;

    protected AbstractDocumentDiagnosticSource(TDocument document)
    {
        this.Document = document;
    }

    public ProjectOrDocumentId GetId() => new(Document.Id);
    public Project GetProject() => Document.Project;
    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(Document.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Document.Project), Uri = Document.GetURI() }
            : null;

    public string ToDisplayString() => $"{Document.FilePath ?? Document.Name} in {Document.Project.Name}";

    protected abstract bool IncludeTaskListItems { get; }
    protected abstract bool IncludeStandardDiagnostics { get; }

    protected abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsWorkerAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken);

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken)
    {
        var taskListItems = IncludeTaskListItems ? await this.GetTaskListDiagnosticsAsync(cancellationToken).ConfigureAwait(false) : ImmutableArray<DiagnosticData>.Empty;
        var diagnostics = IncludeStandardDiagnostics ? await this.GetDiagnosticsWorkerAsync(diagnosticAnalyzerService, context, cancellationToken).ConfigureAwait(false) : ImmutableArray<DiagnosticData>.Empty;
        return taskListItems.AddRange(diagnostics);
    }

    private async Task<ImmutableArray<DiagnosticData>> GetTaskListDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (this.Document is not Document document)
            return ImmutableArray<DiagnosticData>.Empty;

        var service = document.GetLanguageService<ITaskListService>();
        if (service == null)
            return ImmutableArray<DiagnosticData>.Empty;

        var tokenList = document.Project.Solution.Options.GetOption(TaskListOptionsStorage.Descriptors);
        var descriptors = GetAndCacheDescriptors(tokenList);

        var items = await service.GetTaskListItemsAsync(document, descriptors, cancellationToken).ConfigureAwait(false);
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
            projectId: document.Project.Id,
            language: document.Project.Language,
            location: new DiagnosticDataLocation(i.Span, document.Id, mappedFileSpan: i.MappedSpan)));
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
