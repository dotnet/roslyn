﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    internal abstract class FrameViewModel
    {
        private readonly IClassificationFormatMap _formatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;

        public abstract bool ShowMouseOver { get; }

        public FrameViewModel(IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        {
            _formatMap = formatMap;
            _classificationTypeMap = typeMap;
        }

        public ImmutableArray<Inline> Inlines => CreateInlines().ToImmutableArray();

        protected abstract IEnumerable<Inline> CreateInlines();

        protected Run MakeClassifiedRun(string classificationName, string text)
        {
            var classifiedText = new ClassifiedText(classificationName, text);
            return classifiedText.ToRun(_formatMap, _classificationTypeMap);
        }
    }
}
