// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

[Export(typeof(ITaggerProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TagType(typeof(ITextMarkerTag))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RenameTaggerProvider(InlineRenameService renameService) : ITaggerProvider
{
    private readonly InlineRenameService _renameService = renameService;

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        => new RenameTagger(buffer, _renameService) as ITagger<T>;
}
