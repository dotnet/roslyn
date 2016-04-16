// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(ITextMarkerTag))]
    internal class RenameTaggerProvider : ITaggerProvider
    {
        private readonly InlineRenameService _renameService;

        [ImportingConstructor]
        public RenameTaggerProvider(InlineRenameService renameService)
        {
            _renameService = renameService;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new RenameTagger(buffer, _renameService) as ITagger<T>;
        }
    }
}
