﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
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
