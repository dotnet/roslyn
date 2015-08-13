// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TagType(typeof(IClassificationTag))]
    internal partial class SyntacticClassificationTaggerProvider : ITaggerProvider
    {
        private readonly IForegroundNotificationService _notificationService;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;
        private readonly ClassificationTypeMap _typeMap;

        private readonly ConditionalWeakTable<ITextBuffer, TagComputer> _tagComputers = new ConditionalWeakTable<ITextBuffer, TagComputer>();

        [ImportingConstructor]
        public SyntacticClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            ClassificationTypeMap typeMap,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _notificationService = notificationService;
            _typeMap = typeMap;
            _asyncListeners = asyncListeners;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (!buffer.GetOption(InternalFeatureOnOffOptions.SyntacticColorizer))
            {
                return null;
            }

            TagComputer tagComputer;
            if (!_tagComputers.TryGetValue(buffer, out tagComputer))
            {
                var asyncListener = new AggregateAsynchronousOperationListener(_asyncListeners, FeatureAttribute.Classification);
                tagComputer = new TagComputer(buffer, _notificationService, asyncListener, _typeMap, this);
                _tagComputers.Add(buffer, tagComputer);
            }

            tagComputer.IncrementReferenceCount();

            var tagger = new Tagger(tagComputer);
            var typedTagger = tagger as ITagger<T>;

            if (typedTagger == null)
            {
                // Oops, we can't actually return this tagger, so just clean up
                tagger.Dispose();
                return null;
            }
            else
            {
                return typedTagger;
            }
        }

        private void DisconnectTagComputer(ITextBuffer buffer)
        {
            _tagComputers.Remove(buffer);
        }
    }
}
