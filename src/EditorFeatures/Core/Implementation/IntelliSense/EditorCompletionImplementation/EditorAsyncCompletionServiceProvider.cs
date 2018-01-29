// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    [Export(typeof(IAsyncCompletionServiceProvider))]
    [Name("C# and Visual Basic Completion Service Provider")]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal class EditorAsyncCompletionServiceProvider : IAsyncCompletionServiceProvider
    {
        private readonly IAsyncCompletionBroker _broker;
        private EditorAsyncCompletionService _instance;

        [ImportingConstructor]
        public EditorAsyncCompletionServiceProvider(IAsyncCompletionBroker broker)
        {
            _broker = broker;
        }

        public EditorCompletion.IAsyncCompletionService GetOrCreate(ITextView textView)
        {
            if (_instance == null)
            {
                _instance = new EditorAsyncCompletionService(_broker);
            }

            return _instance;
        }
    }
}
