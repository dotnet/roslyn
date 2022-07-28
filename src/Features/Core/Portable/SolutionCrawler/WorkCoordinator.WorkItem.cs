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
            private record class ActiveMemberWithVersions(SyntaxPath ActiveMember, VersionStamp OldVersion, VersionStamp NewVersion);

            // this is internal only type
            private readonly struct WorkItem
            {
                // project related workitem
                public readonly ProjectId ProjectId;

                // document related workitem
                public readonly DocumentId? DocumentId;
                public readonly string Language;
                public readonly InvocationReasons InvocationReasons;
                public readonly bool IsLowPriority;

                // extra info
                public readonly ActiveMemberWithVersions? ActiveMemberWithVersions;

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
                    DocumentId? documentId,
                    ProjectId projectId,
                    string language,
                    InvocationReasons invocationReasons,
                    bool isLowPriority,
                    ActiveMemberWithVersions? activeMemberWithVersions,
                    ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers,
                    bool retry,
                    IAsyncToken asyncToken)
                {
                    Debug.Assert(documentId == null || documentId.ProjectId == projectId);

                    DocumentId = documentId;
                    ProjectId = projectId;
                    Language = language;
                    InvocationReasons = invocationReasons;
                    IsLowPriority = isLowPriority;

                    ActiveMemberWithVersions = activeMemberWithVersions;
                    SpecificAnalyzers = specificAnalyzers;

                    IsRetry = retry;

                    AsyncToken = asyncToken;
                }

                public WorkItem(DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority, ActiveMemberWithVersions? activeMemberWithVersions, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, activeMemberWithVersions, ImmutableHashSet.Create<IIncrementalAnalyzer>(), retry: false, asyncToken)
                {
                }

                public WorkItem(DocumentId documentId, string language, InvocationReasons invocationReasons, bool isLowPriority, IIncrementalAnalyzer? analyzer, IAsyncToken asyncToken)
                    : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, activeMemberWithVersions: null,
                           analyzer == null ? ImmutableHashSet.Create<IIncrementalAnalyzer>() : ImmutableHashSet.Create(analyzer),
                           retry: false, asyncToken)
                {
                }

                public object Key => DocumentId ?? (object)ProjectId;

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
                        DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMemberWithVersions, SpecificAnalyzers,
                        retry: true, asyncToken: asyncToken);
                }

                public WorkItem With(
                    InvocationReasons invocationReasons, ActiveMemberWithVersions? currentMember,
                    ImmutableHashSet<IIncrementalAnalyzer> analyzers, bool retry, IAsyncToken asyncToken)
                {
                    // dispose old one
                    AsyncToken.Dispose();

                    ActiveMemberWithVersions? newActiveMember;
                    if (currentMember?.ActiveMember == ActiveMemberWithVersions?.ActiveMember)
                    {
                        if (currentMember == ActiveMemberWithVersions)
                        {
                            newActiveMember = currentMember;
                        }
                        else
                        {
                            // We have a newer work item for an edit to the same member node.
                            // We create a replacement work item with the following versions:
                            //  1. NewVersion = newer of the two NewVersions
                            //  2. OldVersion = older of the two OldVersions
                            var newVersion = currentMember!.NewVersion.GetNewerVersion(ActiveMemberWithVersions!.NewVersion);
                            var oldVersion = currentMember.OldVersion.GetNewerVersion(ActiveMemberWithVersions.OldVersion) == ActiveMemberWithVersions.OldVersion
                                ? currentMember.OldVersion
                                : ActiveMemberWithVersions.OldVersion;
                            newActiveMember = new ActiveMemberWithVersions(currentMember.ActiveMember, oldVersion, newVersion);
                        }
                    }
                    else
                    {
                        newActiveMember = null;
                    }

                    // create new work item
                    return new WorkItem(
                        DocumentId, ProjectId, Language,
                        InvocationReasons.With(invocationReasons),
                        IsLowPriority,
                        newActiveMember,
                        Union(analyzers), IsRetry || retry,
                        asyncToken);
                }

                public WorkItem WithAsyncToken(IAsyncToken asyncToken)
                {
                    return new WorkItem(
                        DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMemberWithVersions, SpecificAnalyzers,
                        retry: false, asyncToken: asyncToken);
                }

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
                        ActiveMemberWithVersions,
                        SpecificAnalyzers,
                        IsRetry,
                        asyncToken);
                }

                public WorkItem With(ImmutableHashSet<IIncrementalAnalyzer> specificAnalyzers, IAsyncToken asyncToken)
                {
                    return new WorkItem(DocumentId, ProjectId, Language, InvocationReasons,
                        IsLowPriority, ActiveMemberWithVersions, specificAnalyzers, IsRetry, asyncToken);
                }

                public override string ToString()
                    => $"{DocumentId?.ToString() ?? ProjectId.ToString()}, ({InvocationReasons}), LowPriority:{IsLowPriority}, ActiveMember:{ActiveMemberWithVersions != null}, Retry:{IsRetry}, ({string.Join("|", SpecificAnalyzers.Select(a => a.GetType().Name))})";
            }
        }
    }
}
