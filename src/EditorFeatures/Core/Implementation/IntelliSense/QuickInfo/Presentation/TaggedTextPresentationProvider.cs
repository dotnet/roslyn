// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Text;
using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    [ExportQuickInfoPresentationProvider(QuickInfoElementKinds.Text)]
    internal class TaggedTextPresentationProvider : QuickInfoPresentationProvider
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public TaggedTextPresentationProvider(
            ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public override FrameworkElement CreatePresentation(QuickInfoElement element, ITextSnapshot snapshot)
        {
            var classifiedTextBlock = element.Text.ToTextBlock(_typeMap);

            if (classifiedTextBlock.Inlines.Count == 0)
            {
                classifiedTextBlock.Visibility = Visibility.Collapsed;
            }

            return classifiedTextBlock;
        }
    }
}