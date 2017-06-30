﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    /// <summary>
    /// Order after "squiggle" so that we have the opportunity to remove any quick info content
    /// added due to errors in the code (which happen right after "eventName +=")
    /// </summary>
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name(PredefinedQuickInfoProviderNames.EventHookup)]
    [Order(After = PredefinedQuickInfoProviderNames.Semantic)]
    [Order(After = PredefinedQuickInfoProviderNames.Syntactic)]
    [Order(After = "squiggle")]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal sealed class EventHookupQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        private readonly ClassificationTypeMap _classificationTypeMap;

        [ImportingConstructor]
        public EventHookupQuickInfoSourceProvider(ClassificationTypeMap classificationTypeMap)
        {
            _classificationTypeMap = classificationTypeMap;
        }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<EventHookupQuickInfoSource>(() => new EventHookupQuickInfoSource(textBuffer, _classificationTypeMap));
        }
    }
}
