//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
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
