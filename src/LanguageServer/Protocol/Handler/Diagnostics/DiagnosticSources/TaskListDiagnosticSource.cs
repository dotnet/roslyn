// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

using static PullDiagnosticConstants;

internal sealed class TaskListDiagnosticSource(Document document, IGlobalOptionService globalOptions) : AbstractDocumentDiagnosticSource<Document>(document)
{
    private static readonly ImmutableArray<string> s_todoCommentCustomTags = [TaskItemCustomTag];

    private static readonly ImmutableDictionary<string, string?> s_lowPriorityProperties = ImmutableDictionary<string, string?>.Empty.Add(Priority, Low);
    private static readonly ImmutableDictionary<string, string?> s_mediumPriorityProperties = ImmutableDictionary<string, string?>.Empty.Add(Priority, Medium);
    private static readonly ImmutableDictionary<string, string?> s_highPriorityProperties = ImmutableDictionary<string, string?>.Empty.Add(Priority, High);

    private static Tuple<ImmutableArray<string>, ImmutableArray<TaskListItemDescriptor>> s_lastRequestedTokens =
        Tuple.Create(ImmutableArray<string>.Empty, ImmutableArray<TaskListItemDescriptor>.Empty);

    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public override bool IsLiveSource()
        => true;

    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        RequestContext context, CancellationToken cancellationToken)
    {
        var service = this.Document.GetLanguageService<ITaskListService>();
        if (service == null)
            return [];

        var options = _globalOptions.GetTaskListOptions();
        var descriptors = GetAndCacheDescriptors(options.Descriptors);

        var items = await service.GetTaskListItemsAsync(this.Document, descriptors, cancellationToken).ConfigureAwait(false);
        if (items.Length == 0)
            return [];

        return items.SelectAsArray(i => new DiagnosticData(
            id: "TODO",
            category: "TODO",
            message: i.Message,
            severity: DiagnosticSeverity.Info,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            warningLevel: 0,
            customTags: s_todoCommentCustomTags,
            properties: GetProperties(i.Priority),
            projectId: this.Document.Project.Id,
            language: this.Document.Project.Language,
            location: new DiagnosticDataLocation(i.Span, this.Document.Id, mappedFileSpan: i.MappedSpan)));
    }

    private static ImmutableDictionary<string, string?> GetProperties(TaskListItemPriority priority)
        => priority switch
        {
            TaskListItemPriority.Low => s_lowPriorityProperties,
            TaskListItemPriority.Medium => s_mediumPriorityProperties,
            TaskListItemPriority.High => s_highPriorityProperties,
            _ => s_mediumPriorityProperties,
        };

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
