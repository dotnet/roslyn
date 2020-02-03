// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Rename
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
