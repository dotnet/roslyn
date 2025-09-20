// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests;

public class FileSet : IEnumerable<(string fileName, object content)>
{
    private readonly IImmutableDictionary<string, object> _fileMap;

    private FileSet(IImmutableDictionary<string, object> fileMap)
    {
        _fileMap = fileMap ?? ImmutableDictionary<string, object>.Empty;
    }

    public FileSet(params (string fileName, object content)[] files)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var (fileName, fileContent) in files)
        {
            builder.Add(fileName, fileContent);
        }

        _fileMap = builder.ToImmutable();
    }

    public IEnumerator<(string fileName, object content)> GetEnumerator()
    {
        foreach (var (fileName, content) in _fileMap)
            yield return (fileName, content);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public FileSet WithFile(string fileName, object content)
    {
        var newFileMap = _fileMap.SetItem(fileName, content);

        return new FileSet(newFileMap);
    }

    public FileSet ReplaceFileElement(string fileName, string elementName, string elementValue)
    {
        if (_fileMap.TryGetValue(fileName, out var content))
        {
            if (content is string textContent)
            {
                var elementStartTag = "<" + elementName;
                var elementEndTag = "</" + elementName;
                var startTagStart = textContent.IndexOf(elementStartTag, StringComparison.Ordinal);
                if (startTagStart >= -1)
                {
                    var startTagEnd = textContent.IndexOf('>', startTagStart + 1);
                    if (startTagEnd >= startTagStart)
                    {
                        var endTagStart = textContent.IndexOf(elementEndTag, startTagEnd + 1, StringComparison.Ordinal);
                        if (endTagStart >= startTagEnd)
                        {
                            var newContent = textContent[..(startTagEnd + 1)] + elementValue + textContent[endTagStart..];
                            return this.WithFile(fileName, newContent);
                        }
                    }
                }
            }
        }

        return this;
    }
}
