// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(IClassificationTag))]
    internal class RenameClassificationTaggerProvider : ITaggerProvider
    {
        private readonly InlineRenameService _renameService;
        private readonly IClassificationType _classificationType;

        [ImportingConstructor]
        public RenameClassificationTaggerProvider(
            InlineRenameService renameService,
            IClassificationTypeRegistryService classificationTypeRegistryService)
        {
            _renameService = renameService;
            _classificationType = classificationTypeRegistryService.GetClassificationType(ClassificationTypeDefinitions.InlineRenameField);
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new RenameClassificationTagger(buffer, _renameService, _classificationType) as ITagger<T>;
        }
    }
}
