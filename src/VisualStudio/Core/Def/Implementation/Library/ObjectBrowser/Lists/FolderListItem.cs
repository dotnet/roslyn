// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal class FolderListItem : ObjectListItem
    {
        private readonly string _displayText;

        public FolderListItem(ProjectId projectId, string displayText)
            : base(projectId, StandardGlyphGroup.GlyphClosedFolder)
        {
            _displayText = displayText;
        }

        public override string DisplayText => _displayText;

        public override string FullNameText => _displayText;

        public override string SearchText => _displayText;
    }
}
