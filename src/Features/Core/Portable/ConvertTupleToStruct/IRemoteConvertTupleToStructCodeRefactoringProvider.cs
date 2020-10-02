// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertTupleToStruct
{
    internal interface IRemoteConvertTupleToStructCodeRefactoringProvider
    {
        Task<SerializableConvertTupleToStructResult> ConvertToStructAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            TextSpan span,
            Scope scope,
            CancellationToken cancellationToken);
    }

    internal class SerializableConvertTupleToStructResult
    {
        public (DocumentId, TextChange[])[] DocumentTextChanges;
        public (DocumentId, TextSpan) RenamedToken;
    }
}
