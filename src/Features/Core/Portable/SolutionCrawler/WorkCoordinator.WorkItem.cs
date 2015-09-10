// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            // this is internal only type
            private struct WorkItem
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
                public readonly ImmutableHashSet<IIncrementalAnalyzer> Analyzers;

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
                    ImmutableHashSet<IIncrementalAnalyzer> analyzers,
                    bool retry,
                    IAsyncToken asyncToken)
                {
                    this.DocumentId = documentId;
                    this.ProjectId = projectId;
                    this.Language = language;
                    this.InvocationReasons = invocationReasons;
                    this.IsLowPriority = isLowPriority;

                    this.ActiveMember = activeMember;
                    this.Analyzers = analyzers;

                    this.IsRetry = retry;

                    this.AsyncToken = asyncToken;
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
                    if (this.Analyzers.IsEmpty && analyzers.IsEmpty)
                    {
                        return this.Analyzers;
                    }

                    if (this.Analyzers.IsEmpty && !analyzers.IsEmpty)
                    {
                        return analyzers;
                    }

                    if (!this.Analyzers.IsEmpty && analyzers.IsEmpty)
                    {
                        return this.Analyzers;
                    }

                    return this.Analyzers.Union(analyzers);
                }

                public WorkItem Retry(IAsyncToken asyncToken)
                {
                    return new WorkItem(
                        this.DocumentId, this.ProjectId, this.Language, this.InvocationReasons, this.IsLowPriority, this.ActiveMember, this.Analyzers,
                        retry: true, asyncToken: asyncToken);
                }

                public WorkItem With(
                    InvocationReasons invocationReasons, SyntaxPath currentMember,
                    ImmutableHashSet<IIncrementalAnalyzer> analyzers, bool retry, IAsyncToken asyncToken)
                {
                    // dispose old one
                    this.AsyncToken.Dispose();

                    // create new work item
                    return new WorkItem(
                        this.DocumentId, this.ProjectId, this.Language,
                        InvocationReasons.With(invocationReasons),
                        IsLowPriority,
                        this.ActiveMember == currentMember ? currentMember : null,
                        Union(analyzers), this.IsRetry || retry,
                        asyncToken);
                }

                public WorkItem With(IAsyncToken asyncToken)
                {
                    return new WorkItem(
                        this.DocumentId, this.ProjectId, this.Language, this.InvocationReasons, this.IsLowPriority, this.ActiveMember, this.Analyzers,
                        retry: false, asyncToken: asyncToken);
                }

                public WorkItem With(DocumentId documentId, ProjectId projectId, IAsyncToken asyncToken)
                {
                    // create new work item
                    return new WorkItem(
                        documentId,
                        projectId,
                        this.Language,
                        this.InvocationReasons,
                        this.IsLowPriority,
                        this.ActiveMember,
                        this.Analyzers,
                        this.IsRetry,
                        asyncToken);
                }
            }
        }
    }
}
