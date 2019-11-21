// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            // this is internal only type
            private readonly struct WorkItem
            {
                // project related workitem
                public readonly ProjectId ProjectId;

                // document related workitem
                public readonly DocumentId DocumentId;
                public readonly string Language;
                public readonly InvocationReasons InvocationReasons;
                public readonly bool IsLowPriority;

                // extra info
                public readonly SyntaxPath ActiveMember;

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

                // retry
                public readonly bool IsRetry;

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
                    DocumentId documentId,
                    ProjectId projectId,
                    string language,
                    InvocationReasons invocationReasons,
                    bool isLowPriority,
                    SyntaxPath activeMember,
                    ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers,
                    bool retry,
                    IAsyncToken asyncToken)
                {
                    DocumentId = documentId;
                    ProjectId = projectId;
                    Language = language;
                    InvocationReasons = invocationReasons;
                    IsLowPriority = isLowPriority;

                    ActiveMember = activeMember;
                    SpecificAnalyzers = specificAnalyzers;

                    IsRetry = retry;

                    AsyncToken = asyncToken;
                }

                public WorkItem(DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, null, ImmutableHashSet.Create<IIncrementalAnalyzer>(), false, asyncToken)
                {
                }

                public WorkItem(
                    DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority,
                    SyntaxPath activeMember, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority,
                           activeMember, ImmutableHashSet.Create<IIncrementalAnalyzer>(),
                           false, asyncToken)
                {
                }

                public WorkItem(
                    DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority,
                    IIncrementalAnalyzer analyzer, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority,
                           null, analyzer == null ? ImmutableHashSet.Create<IIncrementalAnalyzer>() : ImmutableHashSet.Create<IIncrementalAnalyzer>(analyzer),
                           false, asyncToken)
                {
                }

                public object Key
                {
                    get { return DocumentId ?? (object)ProjectId; }
                }

                private ImmutableHashSet<IIncrementalAnalyzer> Union(ImmutableHashSet<IIncrementalAnalyzer> analyzers)
                {
                    if (analyzers.IsEmpty)
                    {
                        return SpecificAnalyzers;
                    }

                    if (SpecificAnalyzers.IsEmpty)
                    {
                        return analyzers;
                    }

                    return SpecificAnalyzers.Union(analyzers);
                }

                public WorkItem Retry(IAsyncToken asyncToken)
                {
                    return new WorkItem(
                        DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMember, SpecificAnalyzers,
                        retry: true, asyncToken: asyncToken);
                }

                public WorkItem With(
                    InvocationReasons invocationReasons, SyntaxPath currentMember,
                    ImmutableHashSet<IIncrementalAnalyzer> analyzers, bool retry, IAsyncToken asyncToken)
                {
                    // dispose old one
                    AsyncToken.Dispose();

                    // create new work item
                    return new WorkItem(
                        DocumentId, ProjectId, Language,
                        InvocationReasons.With(invocationReasons),
                        IsLowPriority,
                        ActiveMember == currentMember ? currentMember : null,
                        Union(analyzers), IsRetry || retry,
                        asyncToken);
                }

                public WorkItem With(IAsyncToken asyncToken)
                {
                    return new WorkItem(
                        DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMember, SpecificAnalyzers,
                        retry: false, asyncToken: asyncToken);
                }

                public WorkItem With(DocumentId documentId, ProjectId projectId, IAsyncToken asyncToken)
                {
                    // create new work item
                    return new WorkItem(
                        documentId,
                        projectId,
                        Language,
                        InvocationReasons,
                        IsLowPriority,
                        ActiveMember,
                        SpecificAnalyzers,
                        IsRetry,
                        asyncToken);
                }

                public WorkItem With(ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers, IAsyncToken asyncToken)
                {
                    return new WorkItem(DocumentId, ProjectId, Language, InvocationReasons,
                        IsLowPriority, ActiveMember, specificAnalyzers, IsRetry, asyncToken);
                }

                public override string ToString()
                {
                    return $"{DocumentId?.ToString() ?? ProjectId.ToString()}, ({InvocationReasons.ToString()}), LowPriority:{IsLowPriority}, ActiveMember:{ActiveMember != null}, Retry:{IsRetry}, ({string.Join("|", SpecificAnalyzers.Select(a => a.GetType().Name))})";
                }
            }
        }
    }
}
