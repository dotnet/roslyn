// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

public class TestEdit
{
    public TestEdit(SourceChange change, ITextSnapshot oldSnapshot, ITextSnapshot newSnapshot)
    {
        Change = change;
        OldSnapshot = oldSnapshot;
        NewSnapshot = newSnapshot;
    }

    public TestEdit(int position, int oldLength, ITextSnapshot oldSnapshot, ITextSnapshot newSnapshot, string newText)
    {
        Change = new SourceChange(position, oldLength, newText);
        OldSnapshot = oldSnapshot;
        NewSnapshot = newSnapshot;
    }

    public SourceChange Change { get; }

    public ITextSnapshot OldSnapshot { get; }

    public ITextSnapshot NewSnapshot { get; }
}
