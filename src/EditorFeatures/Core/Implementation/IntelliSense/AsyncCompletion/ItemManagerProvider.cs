// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionItemManagerProvider))]
    [Name("Roslyn Completion Service Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class ItemManagerProvider : IAsyncCompletionItemManagerProvider
    {
        private readonly ItemManager _instance;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ItemManagerProvider(RecentItemsManager recentItemsManager)
            => _instance = new ItemManager(recentItemsManager);

        public IAsyncCompletionItemManager GetOrCreate(ITextView textView)
        {
            if (textView.TextBuffer.TryGetWorkspace(out var workspace))
            {
                var workspaceContextService = workspace.Services.GetRequiredService<IWorkspaceContextService>();

                // If we're in a cloud environment context, we want to avoid returning a completion item manager.
                // Otherwise, we'll interfere with the LSP client manager and disrupt filtering.
                if (workspaceContextService.IsCloudEnvironmentClient())
                {
                    return null;
                }
            }

            return _instance;
        }
    }
}
