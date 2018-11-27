﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CommitManagerProvider(IThreadingContext threadingContext, RecentItemsManager recentItemsManager)
        {
            _threadingContext = threadingContext;
            _recentItemsManager = recentItemsManager;
        }

        IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
        {
            if (!textView.TextBuffer.Properties.TryGetProperty(CompletionSource.PotentialCommitCharacters, out ImmutableArray<char> potentialCommitCharacters))
            {
                // If we were not initialized with CompletionService or are called for a wrong textView, we should not make a commit.
                potentialCommitCharacters = ImmutableArray<char>.Empty;
            }

            return new CommitManager(potentialCommitCharacters, _recentItemsManager, _threadingContext);
        }
    }
}
