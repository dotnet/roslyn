// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionItemSourceProvider))]
    [Name("C# and Visual Basic Item Source Provider")]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CompletionItemSourceProvider : IAsyncCompletionItemSourceProvider
    {
        CompletionItemSource _instance;

        IAsyncCompletionItemSource IAsyncCompletionItemSourceProvider.GetOrCreate(ITextView textView)
        {
            if (_instance == null)
                _instance = new CompletionItemSource();
            return _instance;
        }
    }
}
