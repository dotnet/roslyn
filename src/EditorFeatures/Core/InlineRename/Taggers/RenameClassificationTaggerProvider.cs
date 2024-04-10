// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

[Export(typeof(ITaggerProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[TagType(typeof(IClassificationTag))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RenameClassificationTaggerProvider(
    InlineRenameService renameService,
    IClassificationTypeRegistryService classificationTypeRegistryService) : ITaggerProvider
{
    private readonly InlineRenameService _renameService = renameService;
    private readonly IClassificationType _classificationType = classificationTypeRegistryService.GetClassificationType(ClassificationTypeDefinitions.InlineRenameField);

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        => new RenameClassificationTagger(buffer, _renameService, _classificationType) as ITagger<T>;
}
