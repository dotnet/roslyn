// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("Roslyn Completion Commit Manager")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CommitManagerProvider : IAsyncCompletionCommitManagerProvider
    {
        private readonly IAsyncCompletionCommitManager _instance = new CommitManager();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CommitManagerProvider()
        {
        }

        IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
        {
            return _instance;
        }
    }
}
