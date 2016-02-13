// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    internal class FieldInitializerItem : ICallHierarchyNameItem
    {
        private readonly IEnumerable<ICallHierarchyItemDetails> _details;
        private readonly ImageSource _displayGlyph;
        private readonly string _name;
        private readonly string _sortText;

        public FieldInitializerItem(string name, string sortText, ImageSource displayGlyph, IEnumerable<CallHierarchyDetail> details)
        {
            _name = name;
            _sortText = sortText;
            _displayGlyph = displayGlyph;
            _details = details;
        }

        public IEnumerable<ICallHierarchyItemDetails> Details
        {
            get
            {
                return _details;
            }
        }

        public ImageSource DisplayGlyph
        {
            get
            {
                return _displayGlyph;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string SortText
        {
            get
            {
                return _sortText;
            }
        }
    }
}
