// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

internal class FieldInitializerItem : ICallHierarchyNameItem
{
    public FieldInitializerItem(string name, string sortText, ImageSource displayGlyph, IEnumerable<CallHierarchyDetail> details)
    {
        Name = name;
        SortText = sortText;
        DisplayGlyph = displayGlyph;
        Details = details;
    }

    public IEnumerable<ICallHierarchyItemDetails> Details { get; }

    public ImageSource DisplayGlyph { get; }

    public string Name { get; }

    public string SortText { get; }
}
