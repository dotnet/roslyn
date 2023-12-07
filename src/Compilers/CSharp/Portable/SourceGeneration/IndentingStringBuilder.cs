// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// A helper type for generating C# source code efficiently.  Content written is added to a <see cref="StringBuilder"/>,
/// with default behavior provided for controlling the current indent level of the code.
/// </summary>
/// <remarks>
/// Not threadsafe.
/// </remarks>
internal struct IndentingStringBuilder : IDisposable
{
    private const string DefaultIndentation = "    ";
    private const string DefaultEndOfLine = "\r\n";
    private const int DefaultIndentationCount = 8;

    // On net8 we should use SearchValues here.

    /// <summary>
    /// The new line characters accepted by C#.
    /// </summary>
    private static ReadOnlySpan<char> NewLineChars => new[] { '\r', '\n', '\f', '\u0085', '\u2028', '\u2029' };

    private static readonly ImmutableArray<string> s_defaultIndentationStrings;

    static IndentingStringBuilder()
    {
        var builder = ArrayBuilder<string>.GetInstance(DefaultIndentationCount);

        PopulateIndentationStrings(builder, DefaultIndentation);

        s_defaultIndentationStrings = builder.ToImmutableAndFree();
    }

    private PooledStringBuilder? _builder = PooledStringBuilder.GetInstance();

    private readonly ArrayBuilder<string> _indentationStrings = ArrayBuilder<string>.GetInstance();

    private readonly string _indentationString;
    private readonly string _endOfLine;

    /// <summary>
    /// The current indentation level.
    /// </summary>
    private int _currentIndentationLevel = 0;

    /// <summary>
    /// The current indentation, as text.
    /// </summary>
    private string _currentIndentation = "";

    public IndentingStringBuilder(string indentationString, string endOfLine)
    {
        _indentationString = indentationString;
        _endOfLine = endOfLine;

        // Avoid allocating indentation strings in the common case where the client is using the defaults.
        if (indentationString == DefaultIndentation)
        {
            _indentationStrings.AddRange(s_defaultIndentationStrings);
        }
        else
        {
            PopulateIndentationStrings(_indentationStrings, indentationString);
        }
    }

    public static IndentingStringBuilder Create(string indentation = DefaultIndentation, string endOfLine = DefaultEndOfLine)
        => new(indentation, endOfLine);

    private static void PopulateIndentationStrings(ArrayBuilder<string> builder, string indentation)
    {
        builder.Add("");
        for (int i = 1; i < builder.Capacity; i++)
            builder.Add(builder.Last() + indentation);
    }

    [MemberNotNull(nameof(_builder))]
    private void CheckDisposed()
    {
        if (_builder is null)
            throw new ObjectDisposedException(nameof(IndentingStringBuilder));
    }

    private StringBuilder Builder
    {
        get
        {
            CheckDisposed();
            return _builder;
        }
    }

    public void Dispose()
    {
        CheckDisposed();
        _indentationStrings.Free();
        _builder.Free();
        _builder = null!;
    }

    /// <summary>
    /// Increases the current indentation level, increasing the amount of indentation written at the start of a
    /// new line when content is written to this.
    /// </summary>
    public void IncreaseIndent()
    {
        _currentIndentationLevel++;
        if (_currentIndentationLevel == _indentationStrings.Count)
            _indentationStrings.Add(_indentationStrings.Last() + _indentationString);

        _currentIndentation = _indentationStrings[_currentIndentationLevel];
    }

    /// <summary>
    /// Decreases the current indentation level, decreasing the amount of indentation written at the start of a
    /// new line when content is written to it.
    /// </summary>
    public void DecreaseIndent()
    {
        if (_currentIndentationLevel == 0)
            throw new InvalidOperationException($"Current indent is already zero.");

        _currentIndentationLevel--;
        _currentIndentation = _indentationStrings[_currentIndentationLevel];
    }

    /// <summary>
    /// Appends a single end of line sequence to the underlying buffer.  No indentation is written prior to the end of line.
    /// </summary>
    private IndentingStringBuilder AppendEndOfLine()
    {
        this.Builder.Append(_endOfLine);
        return this;
    }

    /// <summary>
    /// Appends a single line to the underlying buffer.  Indentation is written out if the underlying buffer
    /// is at the start of a line.
    /// </summary>
    private void AppendSingleLine(ReadOnlySpan<char> line)
    {
        if (line.Length == 0)
            return;

        var builder = this.Builder;
        if (builder.Length == 0 || NewLineChars.IndexOf(builder[^1]) >= 0)
            builder.Append(_currentIndentation);

        builder.Append(line);
    }

    /// <summary>
    /// Writes content to the underlying buffer.  If the buffer is at the start of a line, then indentation will be
    /// appended first before the content.  By default, for performance reasons, the content is assumed to contain no
    /// newlines in it.  If the content may contain newlines, then <see langword="true"/> should be passed in for
    /// <paramref name="splitContent"/>.  This will cause the provided content to be split into constituent lines,
    /// with each line being appended one at a time.
    /// </summary>
    public IndentingStringBuilder Write(string content, bool splitContent = false)
        => Write(content.AsSpan(), splitContent);

    /// <inheritdoc cref="Write(string, bool)"/>
    public IndentingStringBuilder Write(ReadOnlySpan<char> content, bool splitContent = false)
    {
        if (splitContent)
        {
            while (content.Length > 0)
            {
                var newLineIndex = content.IndexOfAny(NewLineChars);
                if (newLineIndex < 0)
                {
                    // no new line, append the rest of the content to the buffer.
                    AppendSingleLine(content);
                }
                else
                {
                    while (newLineIndex < content.Length && NewLineChars.IndexOf(content[newLineIndex + 1]) >= 0)
                        newLineIndex++;

                    AppendSingleLine(content[0..newLineIndex]);
                    content = content[newLineIndex..];
                }
            }
        }
        else
        {
            AppendSingleLine(content);
        }

        return this;
    }

    /// <summary>
    /// Equivalent to <see cref="Write(string, bool)"/> except that a final end of line sequence will be written after
    /// the content is written.
    /// </summary>
    public IndentingStringBuilder WriteLine(string content, bool splitContent = false)
        => WriteLine(content.AsSpan(), splitContent);

    /// <inheritdoc cref="WriteLine(string, bool)"/>
    public IndentingStringBuilder WriteLine(ReadOnlySpan<char> content, bool splitContent = false)
    {
        Write(content, splitContent);
        AppendEndOfLine();
        return this;
    }

    /// <summary>
    /// Opens a block of code to write new content into.  Can be used like so:
    /// <code>
    /// using (writer.StartBlock())
    /// {
    ///     write.WriteLine("...");
    ///     write.WriteLine("...");
    /// }
    /// </code>
    /// </summary>
    public Block StartBlock()
    {
        this.WriteLine("{");
        this.IncreaseIndent();

        return new Block(this);
    }

    public readonly struct Block(IndentingStringBuilder builder) : IDisposable
    {
        public readonly void Dispose()
        {
            builder.DecreaseIndent();
            builder.WriteLine("}");
        }
    }
}
