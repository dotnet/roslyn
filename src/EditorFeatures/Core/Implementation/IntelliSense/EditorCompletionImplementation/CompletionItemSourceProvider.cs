// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionItemSourceProvider))]
    [Name("C# Item Source Provider")]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpCompletionItemSourceProvider : IAsyncCompletionItemSourceProvider
    {
        CSharpCompletionItemSource _instance;

        IAsyncCompletionItemSource IAsyncCompletionItemSourceProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
                _instance = new CSharpCompletionItemSource();
            return _instance;
        }
    }

    [Export(typeof(IAsyncCompletionItemSourceProvider))]
    [Name("Visual Basic Item Source Provider")]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal class VisualBasicCompletionItemSourceProvider : IAsyncCompletionItemSourceProvider
    {
        VisualBasicCompletionItemSource _instance;

        IAsyncCompletionItemSource IAsyncCompletionItemSourceProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
                _instance = new VisualBasicCompletionItemSource();
            return _instance;
        }
    }
}
