// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionItemManagerProvider))]
    [Name("C# and Visual Basic Completion Service Provider")]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal class EditorAsyncCompletionServiceProvider : IAsyncCompletionItemManagerProvider
    {
        private readonly IAsyncCompletionBroker _broker;
        private IAsyncCompletionItemManager _instance;

        [ImportingConstructor]
        public EditorAsyncCompletionServiceProvider(IAsyncCompletionBroker broker)
        {
            _broker = broker;
        }

        public IAsyncCompletionItemManager GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new EditorAsyncCompletionService(_broker);
            }

            return _instance;
        }
    }
}
