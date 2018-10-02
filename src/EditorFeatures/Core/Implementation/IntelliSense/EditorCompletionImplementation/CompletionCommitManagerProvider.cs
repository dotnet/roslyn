// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("Roslyn Completion Commit Manager")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
    {
        private IAsyncCompletionCommitManager _instance;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionCommitManagerProvider()
        {
        }

        IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new CompletionCommitManager();
            }

            return _instance;
        }
    }
}
