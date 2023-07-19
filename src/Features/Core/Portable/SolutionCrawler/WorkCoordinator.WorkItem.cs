// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        internal partial class WorkCoordinator
        {
            // this is internal only type
            internal readonly struct WorkItem
            {
                // project related workitem
                public readonly ProjectId ProjectId;

                // document related workitem
                public readonly DocumentId? DocumentId;
                public readonly string Language;
                public readonly InvocationReasons InvocationReasons;
                public readonly bool IsLowPriority;

                // extra info
                public readonly SyntaxPath? ActiveMember;

                /// <summary>
                /// Non-empty if this work item is intended to be executed only for specific incremental analyzer(s).
                /// Otherwise, the work item is applicable to all relevant incremental analyzers.
                /// </summary>
                public readonly ImmutableHashSet<IIncrementalAnalyzer> SpecificAnalyzers;

                /// <summary>
                /// Gets all the applicable analyzers to execute for this work item.
                /// If this work item has any specific analyzer(s), then returns the intersection of <see cref="SpecificAnalyzers"/>
                /// and the given <paramref name="allAnalyzers"/>.
                /// Otherwise, returns <paramref name="allAnalyzers"/>.
                /// </summary>
                public IEnumerable<IIncrementalAnalyzer> GetApplicableAnalyzers(ImmutableArray<IIncrementalAnalyzer> allAnalyzers)
                    => SpecificAnalyzers?.Count > 0 ? SpecificAnalyzers.Where(allAnalyzers.Contains) : allAnalyzers;

                // common
                public readonly IAsyncToken AsyncToken;

                public bool MustRefresh
                {
                    get
                    {
                        // in current design, we need to re-run all incremental analyzer on document open and close
                        // so that incremental analyzer who only cares about opened document can have a chance to clean up
                        // its state.
                        return InvocationReasons.Contains(PredefinedInvocationReasons.DocumentOpened) ||
                               InvocationReasons.Contains(PredefinedInvocationReasons.DocumentClosed);
                    }
                }

                private WorkItem(
                    DocumentId? documentId,
                    ProjectId projectId,
                    string language,
                    InvocationReasons invocationReasons,
                    bool isLowPriority,
                    SyntaxPath? activeMember,
                    ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers,
                    IAsyncToken asyncToken)
                {
                    Debug.Assert(documentId == null || documentId.ProjectId == projectId);

                    DocumentId = documentId;
                    ProjectId = projectId;
                    Language = language;
                    InvocationReasons = invocationReasons;
                    IsLowPriority = isLowPriority;

                    ActiveMember = activeMember;
                    SpecificAnalyzers = specificAnalyzers;

                    AsyncToken = asyncToken;
                }

                public WorkItem(DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority, SyntaxPath? activeMember, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, activeMember, ImmutableHashSet.Create<IIncrementalAnalyzer>(), asyncToken)
                {
                }

                public WorkItem(DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority, IIncrementalAnalyzer? analyzer, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, activeMember: null,
                           analyzer == null ? ImmutableHashSet.Create<IIncrementalAnalyzer>() : ImmutableHashSet.Create(analyzer),
                           asyncToken)
                {
                }

                public object Key => DocumentId ?? (object)ProjectId;

                public WorkItem With(
                    InvocationReasons invocationReasons,
                    SyntaxPath? currentMember,
                    ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers,
                    IAsyncToken asyncToken)
                {
                    // dispose old one
                    AsyncToken.Dispose();

                    // create new work item
                    return new WorkItem(
                        DocumentId, ProjectId, Language,
                        InvocationReasons.With(invocationReasons),
                        IsLowPriority,
                        ActiveMember == currentMember ? currentMember : null,
                        ComputeNewSpecificAnalyzers(specificAnalyzers, SpecificAnalyzers),
                        asyncToken);

                    static ImmutableHashSet<IIncrementalAnalyzer> ComputeNewSpecificAnalyzers(ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers1, ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers2)
                    {
                        // An empty analyzer list means run all analyzers, so empty always wins over any specific
                        if (specificAnalyzers1.IsEmpty || specificAnalyzers2.IsEmpty)
                        {
                            return ImmutableHashSet<IIncrementalAnalyzer>.Empty;
                        }

                        // Otherwise, if both sets have analyzers we use a union of the two
                        return specificAnalyzers1.Union(specificAnalyzers2);
                    }
                }

                public WorkItem WithAsyncToken(IAsyncToken asyncToken)
                    => new(DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMember, SpecificAnalyzers, asyncToken);

                public WorkItem ToProjectWorkItem(IAsyncToken asyncToken)
                {
                    RoslynDebug.Assert(DocumentId != null);

                    // create new work item that represents work per project
                    return new WorkItem(
                        documentId: null,
                        DocumentId.ProjectId,
                        Language,
                        InvocationReasons,
                        IsLowPriority,
                        ActiveMember,
                        SpecificAnalyzers,
                        asyncToken);
                }

                public override string ToString()
                    => $"{DocumentId?.ToString() ?? ProjectId.ToString()}, ({InvocationReasons}), LowPriority:{IsLowPriority}, ActiveMember:{ActiveMember != null}, ({string.Join("|", SpecificAnalyzers.Select(a => a.GetType().Name))})";
            }
        }
    }
}
