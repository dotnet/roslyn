// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        public override string DisplayText
        {
            get { return _displayText; }
        }

        public override string FullNameText
        {
            get { return _displayText; }
        }

        public override string SearchText
        {
            get { return _displayText; }
        }
    }
}
