// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal sealed partial class UnitTestingSolutionCrawlerRegistrationService
{
    internal sealed partial class UnitTestingWorkCoordinator
    {
        // this is internal only type
        private readonly struct UnitTestingWorkItem
        {
            // project related workitem
            public readonly ProjectId ProjectId;

            // document related workitem
            public readonly DocumentId? DocumentId;
            public readonly string Language;
            public readonly UnitTestingInvocationReasons InvocationReasons;
            public readonly bool IsLowPriority;

            // extra info
            public readonly SyntaxPath? ActiveMember;

            /// <summary>
            /// Non-empty if this work item is intended to be executed only for specific incremental analyzer(s).
            /// Otherwise, the work item is applicable to all relevant incremental analyzers.
            /// </summary>
            public readonly ImmutableHashSet<IUnitTestingIncrementalAnalyzer> SpecificAnalyzers;

            /// <summary>
            /// Gets all the applicable analyzers to execute for this work item.
            /// If this work item has any specific analyzer(s), then returns the intersection of <see cref="SpecificAnalyzers"/>
            /// and the given <paramref name="allAnalyzers"/>.
            /// Otherwise, returns <paramref name="allAnalyzers"/>.
            /// </summary>
            public IEnumerable<IUnitTestingIncrementalAnalyzer> GetApplicableAnalyzers(ImmutableArray<IUnitTestingIncrementalAnalyzer> allAnalyzers)
                => SpecificAnalyzers?.Count > 0 ? SpecificAnalyzers.Where(allAnalyzers.Contains) : allAnalyzers;

            // retry
            public readonly bool IsRetry;

            // common
            public readonly IAsyncToken AsyncToken;

            private UnitTestingWorkItem(
                DocumentId? documentId,
                ProjectId projectId,
                string language,
                UnitTestingInvocationReasons invocationReasons,
                bool isLowPriority,
                SyntaxPath? activeMember,
                ImmutableHashSet<IUnitTestingIncrementalAnalyzer> specificAnalyzers,
                bool retry,
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

                IsRetry = retry;

                AsyncToken = asyncToken;
            }

            public UnitTestingWorkItem(DocumentId documentId, string language, UnitTestingInvocationReasons invocationReasons, bool isLowPriority, SyntaxPath? activeMember, IAsyncToken asyncToken)
                : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, activeMember, [], retry: false, asyncToken)
            {
            }

            public UnitTestingWorkItem(DocumentId documentId, string language, UnitTestingInvocationReasons invocationReasons, bool isLowPriority, IUnitTestingIncrementalAnalyzer? analyzer, IAsyncToken asyncToken)
                : this(documentId, documentId.ProjectId, language, invocationReasons, isLowPriority, activeMember: null,
                       analyzer == null ? [] : [analyzer],
                       retry: false, asyncToken)
            {
            }

            public object Key => DocumentId ?? (object)ProjectId;

            private ImmutableHashSet<IUnitTestingIncrementalAnalyzer> Union(ImmutableHashSet<IUnitTestingIncrementalAnalyzer> analyzers)
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

            public UnitTestingWorkItem Retry(IAsyncToken asyncToken)
            {
                return new UnitTestingWorkItem(
                    DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMember, SpecificAnalyzers,
                    retry: true, asyncToken: asyncToken);
            }

            public UnitTestingWorkItem With(
                UnitTestingInvocationReasons invocationReasons, SyntaxPath? currentMember,
                ImmutableHashSet<IUnitTestingIncrementalAnalyzer> analyzers, bool retry, IAsyncToken asyncToken)
            {
                // dispose old one
                AsyncToken.Dispose();

                // create new work item
                return new UnitTestingWorkItem(
                    DocumentId, ProjectId, Language,
                    InvocationReasons.With(invocationReasons),
                    IsLowPriority,
                    ActiveMember == currentMember ? currentMember : null,
                    Union(analyzers), IsRetry || retry,
                    asyncToken);
            }

            public UnitTestingWorkItem WithAsyncToken(IAsyncToken asyncToken)
            {
                return new UnitTestingWorkItem(
                    DocumentId, ProjectId, Language, InvocationReasons, IsLowPriority, ActiveMember, SpecificAnalyzers,
                    retry: false, asyncToken: asyncToken);
            }

            public UnitTestingWorkItem ToProjectWorkItem(IAsyncToken asyncToken)
            {
                RoslynDebug.Assert(DocumentId != null);

                // create new work item that represents work per project
                return new UnitTestingWorkItem(
                    documentId: null,
                    DocumentId.ProjectId,
                    Language,
                    InvocationReasons,
                    IsLowPriority,
                    ActiveMember,
                    SpecificAnalyzers,
                    IsRetry,
                    asyncToken);
            }

            public UnitTestingWorkItem With(ImmutableHashSet<IUnitTestingIncrementalAnalyzer> specificAnalyzers, IAsyncToken asyncToken)
            {
                return new UnitTestingWorkItem(DocumentId, ProjectId, Language, InvocationReasons,
                    IsLowPriority, ActiveMember, specificAnalyzers, IsRetry, asyncToken);
            }

            public override string ToString()
                => $"{DocumentId?.ToString() ?? ProjectId.ToString()}, ({InvocationReasons}), LowPriority:{IsLowPriority}, ActiveMember:{ActiveMember != null}, Retry:{IsRetry}, ({string.Join("|", SpecificAnalyzers.Select(a => a.GetType().Name))})";
        }
    }
}
