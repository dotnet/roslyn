// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private sealed partial class IncrementalMemberEditAnalyzer
        {
            /// <summary>
            /// Spans of member nodes for incremental analysis.
            /// </summary>
            private readonly record struct MemberSpans(DocumentId DocumentId, VersionStamp Version, ImmutableArray<TextSpan> Spans);

            private readonly object _gate = new();
            private MemberSpans _savedMemberSpans;

            private async Task<ImmutableArray<TextSpan>> GetOrCreateMemberSpansAsync(Document document, VersionStamp version, CancellationToken cancellationToken)
            {
                lock (_gate)
                {
                    if (_savedMemberSpans.DocumentId == document.Id && _savedMemberSpans.Version == version)
                        return _savedMemberSpans.Spans;
                }

                var memberSpans = await CreateMemberSpansAsync(document, version, cancellationToken).ConfigureAwait(false);

                lock (_gate)
                {
                    _savedMemberSpans = new MemberSpans(document.Id, version, memberSpans);
                }

                return memberSpans;

                static async Task<ImmutableArray<TextSpan>> CreateMemberSpansAsync(Document document, VersionStamp version, CancellationToken cancellationToken)
                {
                    var service = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    using var pooledMembers = service.GetMethodLevelMembers(root);
                    var members = pooledMembers.Object;

                    return members.SelectAsArray(m => m.FullSpan);
                }
            }

            private void SaveMemberSpans(DocumentId documentId, VersionStamp version, ImmutableArray<TextSpan> memberSpans)
            {
                lock (_gate)
                {
                    _savedMemberSpans = new MemberSpans(documentId, version, memberSpans);
                }
            }
        }
    }
}
