// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Text.Implementation;

internal sealed class SourceTextReader(SourceText sourceText) : TextReader
{
    private readonly SourceText _sourceText = sourceText;
    private int _position = 0;

    public override int Peek()
    {
        return _position == _sourceText.Length
            ? -1
            : _sourceText[_position];
    }

    public override int Read()
    {
        return _position == _sourceText.Length
            ? -1
            : _sourceText[_position++];
    }

    public override int Read(char[] buffer, int index, int count)
    {
        var charsToCopy = Math.Min(count, _sourceText.Length - _position);
        _sourceText.CopyTo(_position, buffer, index, charsToCopy);
        _position += charsToCopy;
        return charsToCopy;
    }
}
