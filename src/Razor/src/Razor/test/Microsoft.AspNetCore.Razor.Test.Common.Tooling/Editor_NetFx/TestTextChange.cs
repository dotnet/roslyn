// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

public class TestTextChange : ITextChange
{
    public TestTextChange(TestEdit edit)
        : this(edit.Change)
    {
    }

    public TestTextChange(SourceChange change)
    {
        var changeSpan = change.Span;

        OldPosition = changeSpan.AbsoluteIndex;
        NewPosition = OldPosition;
        OldEnd = changeSpan.AbsoluteIndex + changeSpan.Length;
        NewEnd = changeSpan.AbsoluteIndex + change.NewText.Length;
    }

    public int OldPosition { get; }

    public int NewPosition { get; }

    public int OldEnd { get; }

    public int NewEnd { get; }

    public Span OldSpan => throw new NotImplementedException();

    public Span NewSpan => throw new NotImplementedException();

    public int Delta => throw new NotImplementedException();

    public string OldText => throw new NotImplementedException();

    public string NewText => throw new NotImplementedException();

    public int OldLength => throw new NotImplementedException();

    public int NewLength => throw new NotImplementedException();

    public int LineCountDelta => throw new NotImplementedException();
}
