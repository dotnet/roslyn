// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionItemManagerProvider))]
    [Name("Roslyn Completion Service Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class ItemManagerProvider(RecentItemsManager recentItemsManager, EditorOptionsService editorOptionsService) : IAsyncCompletionItemManagerProvider
    {
        private readonly ItemManager _instance = new ItemManager(recentItemsManager, editorOptionsService);

        public IAsyncCompletionItemManager? GetOrCreate(ITextView textView)
        {
            if (textView.IsInLspEditorContext())
            {
                // If we're in an LSP editing context, we want to avoid returning a completion item manager.
                // Otherwise, we'll interfere with the LSP client manager and disrupt filtering.
                return null;
            }

            return _instance;
        }
    }
}
