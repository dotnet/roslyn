// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Rename
{
    internal partial class RoslynRenameService : IEditorInlineRenameService
    {
        public Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // TODO: Implement inline rename. Until then, return failure message.
            return Task.FromResult<IInlineRenameInfo>(new FailureInlineRenameInfo("Cannot rename this identifier"));
        }
    }
}
