// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        public IEnumerable<ICallHierarchyItemDetails> Details => _details;

        public ImageSource DisplayGlyph => _displayGlyph;

        public string Name => _name;

        public string SortText => _sortText;
    }
}
