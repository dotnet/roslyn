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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    internal abstract partial class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn> where TDiagnosticsParams : IPartialResultParams<TReport[]>
    {
        protected sealed record class TodoCommentDiagnosticSource(Document Document) : IDiagnosticSource
        {
            private static readonly ImmutableArray<string> s_todoCommentCustomTags = ImmutableArray.Create(TaskItemCustomTag);

            private static Tuple<ImmutableArray<string>, ImmutableArray<TodoCommentDescriptor>> s_lastRequestedTokens =
                Tuple.Create(ImmutableArray<string>.Empty, ImmutableArray<TodoCommentDescriptor>.Empty);

            public ProjectOrDocumentId GetId() => new(Document.Id);

            public Project GetProject() => Document.Project;

            public Uri GetUri() => Document.GetURI();

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
                IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            {
                var service = Document.GetLanguageService<ITodoCommentService>();
                if (service == null)
                    return ImmutableArray<DiagnosticData>.Empty;

                var tokenList = Document.Project.Solution.Options.GetOption(TodoCommentOptionsStorage.TokenList);
                var descriptors = GetAndCacheDescriptors(tokenList);

                var comments = await service.GetTodoCommentsAsync(Document, descriptors, cancellationToken).ConfigureAwait(false);
                return comments.SelectAsArray(comment => new DiagnosticData(
                    id: "TODO",
                    category: "TODO",
                    message: comment.Message,
                    severity: /*unused*/DiagnosticSeverity.Info,
                    defaultSeverity:/*unused*/DiagnosticSeverity.Info,
                    isEnabledByDefault: true,
                    warningLevel: /*unused*/0,
                    customTags: s_todoCommentCustomTags,
                    properties: ImmutableDictionary<string, string?>.Empty,
                    projectId: Document.Project.Id,
                    language: Document.Project.Language,
                    location: new DiagnosticDataLocation(Document.Id, new TextSpan(comment.Position, 0))));
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
    }
}
