// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal sealed class SeekableTextReader : TextReader
{
    private readonly string _filePath;
    private int _position;
    private int _current;
    private SourceLocation _location;
    private (TextSpan Span, int LineIndex) _cachedLineInfo;

    public SeekableTextReader(string source, string filePath) : this(SourceText.From(source, checksumAlgorithm: SourceHashAlgorithm.Sha256), filePath)
    {
    }

    public SeekableTextReader(RazorSourceDocument source) : this(source.Text, source.FilePath)
    {
    }

    private SeekableTextReader(SourceText sourceText, string filePath)
    {
        SourceText = sourceText;
        _filePath = filePath;
        _cachedLineInfo = (SourceText.Lines[0].Span, 0);
        UpdateState();
    }

    public SourceLocation Location => _location;

    public int Length => SourceText.Length;

    public int Position
    {
        get { return _position; }
        set
        {
            if (_position != value)
            {
                _position = value;
                UpdateState();
            }
        }
    }

    public SourceText SourceText { get; }

    public override int Read()
    {
        var c = _current;
        _position++;
        UpdateState();
        return c;
    }

    public override int Peek() => _current;

    private void UpdateState()
    {
        if (_cachedLineInfo.Span.Contains(_position))
        {
            _location = new SourceLocation(_filePath, _position, _cachedLineInfo.LineIndex, _position - _cachedLineInfo.Span.Start);
            _current = SourceText[_location.AbsoluteIndex];

            return;
        }

        if (_position < SourceText.Length)
        {
            if (_position >= _cachedLineInfo.Span.End)
            {
                // Try to avoid the GetLocation call by checking if the next line contains the position
                var nextLineIndex = _cachedLineInfo.LineIndex + 1;
                var nextLineSpan = SourceText.Lines[nextLineIndex].Span;

                if (nextLineSpan.Contains(_position))
                {
                    _cachedLineInfo = (nextLineSpan, nextLineIndex);
                    _location = new SourceLocation(_filePath, _position, nextLineIndex, _position - nextLineSpan.Start);
                    _current = SourceText[_location.AbsoluteIndex];

                    return;
                }
            }
            else
            {
                // Try to avoid the GetLocation call by checking if the previous line contains the position
                var prevLineIndex = _cachedLineInfo.LineIndex - 1;
                var prevLineSpan = SourceText.Lines[prevLineIndex].Span;

                if (prevLineSpan.Contains(_position))
                {
                    _cachedLineInfo = (prevLineSpan, prevLineIndex);
                    _location = new SourceLocation(_filePath, _position, prevLineIndex, _position - prevLineSpan.Start);
                    _current = SourceText[_location.AbsoluteIndex];

                    return;
                }
            }

            // The call to GetLocation is expensive
            _location = new SourceLocation(_filePath, _position, SourceText.Lines.GetLinePosition(_position));

            var lineSpan = SourceText.Lines[_location.LineIndex].Span;
            _cachedLineInfo = (lineSpan, _location.LineIndex);

            _current = SourceText[_location.AbsoluteIndex];

            return;
        }

        if (SourceText.Length == 0)
        {
            _location = SourceLocation.Zero;
            _current = -1;

            return;
        }

        var lineNumber = SourceText.Lines.Count - 1;
        _location = new SourceLocation(_filePath, Length, lineNumber, SourceText.Lines[lineNumber].Span.Length);

        _current = -1;
    }
}
