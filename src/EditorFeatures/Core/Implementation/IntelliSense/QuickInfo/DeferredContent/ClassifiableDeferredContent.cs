// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class ClassifiableDeferredContent : IDeferredQuickInfoContent
    {
        // Internal for testing purposes.
        internal readonly IList<SymbolDisplayPart> ClassifiableContent;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly ClassificationTypeMap _typeMap;

        public ClassifiableDeferredContent(
            IList<SymbolDisplayPart> content,
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            ClassificationTypeMap typeMap)
        {
            this.ClassifiableContent = content;
            _textBufferFactoryService = textBufferFactoryService;
            _contentTypeRegistryService = contentTypeRegistryService;
            _typeMap = typeMap;
        }

        public virtual FrameworkElement Create()
        {
            var classifiedTextBlock = ClassifiableContent.ToTextBlock(_typeMap);

            if (classifiedTextBlock.Inlines.Count == 0)
            {
                classifiedTextBlock.Visibility = Visibility.Collapsed;
            }

            return classifiedTextBlock;
        }
    }
}
