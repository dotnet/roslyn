// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
{
    internal class ProjectListItem : ObjectListItem
    {
        private readonly string _displayText;

        public ProjectListItem(Project project)
            : base(project.Id, GetProjectGlyph(project))
        {
            _displayText = project.GetProjectDisplayName();
        }

        private static StandardGlyphGroup GetProjectGlyph(Project project)
        {
            switch (project.Language)
            {
                case LanguageNames.CSharp:
                    return StandardGlyphGroup.GlyphCoolProject;
                case LanguageNames.VisualBasic:
                    return StandardGlyphGroup.GlyphVBProject;
                default:
                    throw new InvalidOperationException("Unsupported language: " + project.Language);
            }
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
