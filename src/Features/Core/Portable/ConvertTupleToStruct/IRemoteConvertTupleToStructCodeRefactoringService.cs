// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertTupleToStruct;

internal interface IRemoteConvertTupleToStructCodeRefactoringService
{
    internal interface ICallback : IRemoteOptionsCallback<CleanCodeGenerationOptions>
    {
    }

    ValueTask<SerializableConvertTupleToStructResult> ConvertToStructAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        DocumentId documentId,
        TextSpan span,
        Scope scope,
        bool isRecord,
        CancellationToken cancellationToken);
}

[ExportRemoteServiceCallbackDispatcher(typeof(IRemoteConvertTupleToStructCodeRefactoringService)), Shared]
internal sealed class RemoteConvertTupleToStructCodeRefactoringServiceCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteConvertTupleToStructCodeRefactoringService.ICallback
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RemoteConvertTupleToStructCodeRefactoringServiceCallbackDispatcher()
    {
    }

    public ValueTask<CleanCodeGenerationOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken)
        => ((RemoteOptionsProvider<CleanCodeGenerationOptions>)GetCallback(callbackId)).GetOptionsAsync(language, cancellationToken);
}

[DataContract]
internal readonly struct SerializableConvertTupleToStructResult(
    ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> documentTextChanges,
    (DocumentId, TextSpan) renamedToken)
{
    [DataMember(Order = 0)]
    public readonly ImmutableArray<(DocumentId, ImmutableArray<TextChange>)> DocumentTextChanges = documentTextChanges;

    [DataMember(Order = 1)]
    public readonly (DocumentId, TextSpan) RenamedToken = renamedToken;
}
