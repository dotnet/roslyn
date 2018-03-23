// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("C# Item Source Provider")]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpCompletionItemSourceProvider : IAsyncCompletionSourceProvider, IAsyncCompletionCommitManagerProvider
    {
        CSharpCompletionItemSource _instance;

        IAsyncCompletionSource IAsyncCompletionSourceProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new CSharpCompletionItemSource();
            }

            return _instance;
        }

        IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new CSharpCompletionItemSource();
            }

            return _instance;
        }
    }

    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("Visual Basic Item Source Provider")]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal class VisualBasicCompletionItemSourceProvider : IAsyncCompletionSourceProvider, IAsyncCompletionCommitManagerProvider
    {
        VisualBasicCompletionItemSource _instance;

        IAsyncCompletionSource IAsyncCompletionSourceProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new VisualBasicCompletionItemSource();
            }

            return _instance;
        }

        IAsyncCompletionCommitManager IAsyncCompletionCommitManagerProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new VisualBasicCompletionItemSource();
            }

            return _instance;
        }
    }
}
