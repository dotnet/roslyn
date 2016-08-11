// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Text;
using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    [ExportQuickInfoPresentationProvider(QuickInfoElementKinds.Documentation)]
    internal class DocumentationCommentPresentationProvider : QuickInfoPresentationProvider
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public DocumentationCommentPresentationProvider(
            ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public override FrameworkElement CreatePresentation(QuickInfoElement element, ITextSnapshot snapshot)
        {
            var documentationTextBlock = element.Text.ToTextBlock(_typeMap);
            documentationTextBlock.TextWrapping = TextWrapping.Wrap;

            var formatMap = _typeMap.ClassificationFormatMapService.GetClassificationFormatMap("tooltip");
            documentationTextBlock.SetDefaultTextProperties(formatMap);

            // If we have already computed the symbol documentation by now, update
            if (documentationTextBlock.Inlines.Count == 0)
            {
                documentationTextBlock.Visibility = Visibility.Collapsed;
            }

            return documentationTextBlock;
        }
    }
}
