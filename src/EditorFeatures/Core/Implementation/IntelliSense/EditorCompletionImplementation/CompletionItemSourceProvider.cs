// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Roslyn Completion Source Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CompletionItemSourceProvider : IAsyncCompletionSourceProvider
    {
        private IAsyncCompletionSource _instance;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionItemSourceProvider()
        {
        }

        IAsyncCompletionSource IAsyncCompletionSourceProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new CompletionItemSource();
            }

            return _instance;
        }
    }
}
