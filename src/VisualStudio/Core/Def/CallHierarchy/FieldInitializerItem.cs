// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.CallHierarchy;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

internal sealed class FieldInitializerItem : ICallHierarchyNameItem
{
    private readonly IGlyphService _glyphService;

    public FieldInitializerItem(string name, string sortText, IGlyphService glyphService, IEnumerable<CallHierarchyDetail> details)
    {
        Name = name;
        SortText = sortText;
        _glyphService = glyphService;
        Details = details;
    }

    public IEnumerable<ICallHierarchyItemDetails> Details { get; }

    public ImageSource DisplayGlyph => Glyph.FieldPublic.GetImageSource(_glyphService);

    public string Name { get; }

    public string SortText { get; }
}
