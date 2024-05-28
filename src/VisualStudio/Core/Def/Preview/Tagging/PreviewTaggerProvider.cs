// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(HighlightTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
internal class PreviewTaggerProvider : IViewTaggerProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PreviewTaggerProvider()
    {
    }

    public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    {
        if (textView.Properties.TryGetProperty(typeof(PreviewUpdater.PreviewTagger), out PreviewUpdater.PreviewTagger tagger))
        {
            return tagger as ITagger<T>;
        }

        return null;
    }
}
