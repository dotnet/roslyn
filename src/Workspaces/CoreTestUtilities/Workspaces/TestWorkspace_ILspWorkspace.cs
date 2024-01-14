// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public partial class TestWorkspace<TDocument, TProject, TSolution> : ILspWorkspace
    {
        bool ILspWorkspace.SupportsMutation => _supportsLspMutation;

        ValueTask ILspWorkspace.UpdateTextIfPresentAsync(DocumentId documentId, SourceText sourceText, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_supportsLspMutation);
            OnDocumentTextChanged(documentId, sourceText, PreservationMode.PreserveIdentity, requireDocumentPresent: false);
            return ValueTaskFactory.CompletedTask;
        }
    }
}
