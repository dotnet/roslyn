// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertTupleToStruct
{
    internal interface IRemoteConvertTupleToStructCodeRefactoringService
    {
        internal interface ICallback : IRemoteOptionsCallback<CodeCleanupOptions>
        {
        }

        ValueTask<SerializableConvertTupleToStructResult> ConvertToStructAsync(
            PinnedSolutionInfo solutionInfo,
            RemoteServiceCallbackId callbackId,
            DocumentId documentId,
            TextSpan span,
            Scope scope,
            bool isRecord,
            CancellationToken cancellationToken);
    }

    [DataContract]
    internal readonly struct SerializableConvertTupleToStructResult
    {
        [DataMember(Order = 0)]
        public readonly ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> DocumentTextChanges;

        [DataMember(Order = 1)]
        public readonly (DocumentId, TextSpan) RenamedToken;

        public SerializableConvertTupleToStructResult(
            ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> documentTextChanges,
            (DocumentId, TextSpan) renamedToken)
        {
            DocumentTextChanges = documentTextChanges;
            RenamedToken = renamedToken;
        }
    }
}
