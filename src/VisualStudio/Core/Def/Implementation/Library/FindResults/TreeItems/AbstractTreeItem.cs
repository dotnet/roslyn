// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal abstract class AbstractTreeItem
    {
        public IList<AbstractTreeItem> Children { get; protected set; }
        public ushort GlyphIndex { get; protected set; }

        // TODO: Old C# code base has a helper, GetLineTextWithUnicodeDirectionMarkersIfNeeded, which we will need at some point.
        public string DisplayText { get; protected set; }
        public ushort DisplaySelectionStart { get; protected set; }
        public ushort DisplaySelectionLength { get; protected set; }

        public virtual bool UseGrayText
        {
            get
            {
                return this.Children == null || this.Children.Count == 0;
            }
        }

        protected AbstractTreeItem(ushort glyphIndex)
        {
            this.Children = new List<AbstractTreeItem>();
            this.GlyphIndex = glyphIndex;
        }

        public abstract int GoToSource();

        public virtual bool CanGoToReference()
        {
            return false;
        }

        public virtual bool CanGoToDefinition()
        {
            return false;
        }
    }
}