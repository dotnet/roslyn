// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Roslyn Completion Source Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        IAsyncCompletionSource _instance;

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
