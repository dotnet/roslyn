// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    [Export(typeof(IAsyncCompletionItemManagerProvider))]
    [Name("Roslyn Completion Service Provider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class ItemManagerProvider : IAsyncCompletionItemManagerProvider
    {
        private readonly IAsyncCompletionBroker _broker;

        // This is a cheap object to create. We can initialize it in ctor.
        private readonly IAsyncCompletionItemManager _instance;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ItemManagerProvider(IAsyncCompletionBroker broker)
        {
            _broker = broker;
            _instance = new ItemManager(_broker);
        }

        public IAsyncCompletionItemManager GetOrCreate(ITextView textView) => _instance;
    }
}
