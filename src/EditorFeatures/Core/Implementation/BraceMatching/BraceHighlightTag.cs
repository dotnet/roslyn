// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    internal class BraceHighlightTag : TextMarkerTag
    {
        public static readonly BraceHighlightTag StartTag = new BraceHighlightTag(navigateToStart: true);
        public static readonly BraceHighlightTag EndTag = new BraceHighlightTag(navigateToStart: false);

        public bool NavigateToStart { get; }

        private BraceHighlightTag(bool navigateToStart)
            : base(ClassificationTypeDefinitions.BraceMatchingName)
        {
            this.NavigateToStart = navigateToStart;
        }
    }
}
