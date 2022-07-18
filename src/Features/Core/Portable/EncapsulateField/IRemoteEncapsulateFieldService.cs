// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal interface IRemoteEncapsulateFieldService
    {
        // TODO https://github.com/microsoft/vs-streamjsonrpc/issues/789 
        internal interface ICallback // : IRemoteOptionsCallback<CleanCodeGenerationOptions>
        {
            ValueTask<CleanCodeGenerationOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken);
        }

        ValueTask<ImmutableArray<(DocumentId, ImmutableArray<TextChange>)>> EncapsulateFieldsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            DocumentId documentId,
            ImmutableArray<string> fieldSymbolKeys,
            bool updateReferences,
            CancellationToken cancellationToken);
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteEncapsulateFieldService)), Shared]
    internal sealed class RemoteConvertTupleToStructCodeRefactoringServiceCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteEncapsulateFieldService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteConvertTupleToStructCodeRefactoringServiceCallbackDispatcher()
        {
        }

        public ValueTask<CleanCodeGenerationOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken)
            => ((RemoteOptionsProvider<CleanCodeGenerationOptions>)GetCallback(callbackId)).GetOptionsAsync(language, cancellationToken);
    }
}
