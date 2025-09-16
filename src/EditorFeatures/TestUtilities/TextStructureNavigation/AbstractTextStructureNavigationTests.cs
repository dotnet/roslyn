// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.TextStructureNavigation;

public abstract class AbstractTextStructureNavigatorTests
{
    protected abstract string ContentType { get; }
    protected abstract EditorTestWorkspace CreateWorkspace(string code);

    protected StringBuilder result = new();

    protected void AssertExtent(string code)
    {
        using var workspace = CreateWorkspace(code);
        var document = workspace.Documents.First();
        var buffer = document.GetTextBuffer();

        var provider = workspace.GetService<ITextStructureNavigatorProvider>(this.ContentType);

        var navigator = provider.CreateTextStructureNavigator(buffer);

        var position = document.CursorPosition!.Value;
        var extent = navigator.GetExtentOfWord(new SnapshotPoint(buffer.CurrentSnapshot, position));

        var annotatedSpans = document.AnnotatedSpans;

        var (key, expectedSpans) = annotatedSpans.Single();
        Assert.Equal(expectedSpans.Single(), extent.Span.Span.ToTextSpan());

        if (extent.IsSignificant)
        {
            Assert.Equal("Significant", key);
        }
        else
        {
            Assert.Equal("Insignificant", key);
        }
    }
}
