// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("Roslyn Completion Commit Manager")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CommitManagerProvider : IAsyncCompletionCommitManagerProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly RecentItemsManager _recentItemsManager;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CommitManagerProvider(IThreadingContext threadingContext, RecentItemsManager recentItemsManager, IGlobalOptionService globalOptions)
        {
            _threadingContext = threadingContext;
            _recentItemsManager = recentItemsManager;
            _globalOptions = globalOptions;
        }

        IAsyncCompletionCommitManager? IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
        {
            if (textView.TextBuffer.IsInLspEditorContext())
            {
                return null;
            }

            return new CommitManager(textView, _recentItemsManager, _globalOptions, _threadingContext);
        }
    }
}
