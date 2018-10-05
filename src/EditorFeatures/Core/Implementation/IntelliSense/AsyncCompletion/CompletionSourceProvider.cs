// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Roslyn Completion Source Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class CompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        private readonly IAsyncCompletionSource _instance = new CompletionSource();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionSourceProvider()
        {
        }

        public IAsyncCompletionSource GetOrCreate(ITextView textView) => _instance;
    }
}
