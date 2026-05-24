// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal class VisualStudioTextChange : ITextChange
{
    public VisualStudioTextChange(int oldStart, int oldLength, string newText)
    {
        OldSpan = new Span(oldStart, oldLength);
        NewText = newText;
    }

    public VisualStudioTextChange(TextEdit textEdit, ITextSnapshot textSnapshot)
        : this(
            textEdit.Range.Start.Line,
            textEdit.Range.Start.Character,
            textEdit.Range.End.Line,
            textEdit.Range.End.Character,
            textSnapshot,
            textEdit.NewText)
    {
    }

    public VisualStudioTextChange(int startLineNumber, int startCharacter, int endLineNumber, int endCharacter, ITextSnapshot textSnapshot, string newText)
    {
        var startLine = textSnapshot.GetLineFromLineNumber(startLineNumber);
        var startAbsoluteIndex = startLine.Start + startCharacter;
        var endLine = textSnapshot.GetLineFromLineNumber(endLineNumber);
        var endAbsoluteIndex = endLine.Start + endCharacter;
        var length = endAbsoluteIndex - startAbsoluteIndex;
        OldSpan = new Span(startAbsoluteIndex, length);
        NewText = newText;
    }

    public Span OldSpan { get; }
    public int OldPosition => OldSpan.Start;
    public int OldEnd => OldSpan.End;
    public int OldLength => OldSpan.Length;
    public string NewText { get; }
    public int NewLength => NewText.Length;

    public Span NewSpan => throw new NotImplementedException();

    public int NewPosition => throw new NotImplementedException();
    public int Delta => throw new NotImplementedException();
    public int NewEnd => throw new NotImplementedException();
    public string OldText => throw new NotImplementedException();
    public int LineCountDelta => throw new NotImplementedException();

    public override string ToString()
    {
        return OldSpan.ToString() + "->" + NewText;
    }
}
