// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

internal sealed class FieldInitializerItem : ICallHierarchyNameItem
{
    private readonly Func<ImageSource> _glyphCreator;

    public FieldInitializerItem(string name, string sortText, Func<ImageSource> glyphCreator, IEnumerable<CallHierarchyDetail> details)
    {
        Name = name;
        SortText = sortText;
        _glyphCreator = glyphCreator;
        Details = details;
    }

    public IEnumerable<ICallHierarchyItemDetails> Details { get; }

    public ImageSource DisplayGlyph => _glyphCreator();

    public string Name { get; }

    public string SortText { get; }
}
