﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// A helper type for generating C# source code efficiently.  Content written is added to a <see cref="StringBuilder"/>,
/// with default behavior provided for controlling the current indent level of the code.
/// </summary>
/// <remarks>
/// Not thread-safe.
/// </remarks>
internal sealed class IndentingStringBuilder : IDisposable
{
    private const string DefaultIndentation = "    ";
    private const string DefaultEndOfLine = "\r\n";
    private const int DefaultIndentationCount = 8;

    // On net8 we should use SearchValues here.

    /// <summary>
    /// The new line characters accepted by C#.
    /// </summary>
    private static ReadOnlySpan<char> EndOfLineCharacters => ['\r', '\n', '\f', '\u0085', '\u2028', '\u2029'];

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

    public IndentingStringBuilder(string indentationString = DefaultIndentation, string endOfLine = DefaultEndOfLine)
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

        CheckDisposed();
    }

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
    private void AppendSingleLine(ReadOnlySpan<char> line, string? originalLine, bool skipIndent)
    {
        if (line.Length == 0)
            return;

        var builder = this.Builder;

        if (!skipIndent && !IsEndOfLineCharacter(line[0]))
        {
            if (builder.Length == 0 || IsEndOfLineCharacter(builder[^1]))
                builder.Append(_currentIndentation);
        }

#if NET
        builder.Append(line);
#else
        if (originalLine != null)
        {
            builder.Append(originalLine);
        }
        else
        {
            foreach (var ch in line)
                builder.Append(ch);
        }
#endif
    }

    private static bool IsEndOfLineCharacter(char ch)
        => EndOfLineCharacters.IndexOf(ch) >= 0;

    /// <summary>
    /// Writes content to the underlying buffer.  If the buffer is at the start of a line, then indentation will be
    /// appended first before the content.  By default, for performance reasons, the content is assumed to contain no
    /// end of line characters in it.  If the content may contain end of line characters, then <see langword="true"/>
    /// should be passed in for <paramref name="splitContent"/>.  This will cause the provided content to be split into
    /// constituent lines, with each line being appended one at a time.
    /// </summary>
    /// <param name="skipIndent">If true, will not indent content, even if starting a new line.</param>
    public IndentingStringBuilder Write(string content, bool splitContent = false, bool skipIndent = false)
        => Write(content.AsSpan(), content, splitContent, skipIndent);

    /// <inheritdoc cref="Write(string, bool, bool)"/>
    public IndentingStringBuilder Write(ReadOnlySpan<char> content, bool splitContent = false, bool skipIndent = false)
        => Write(content, originalString: null, splitContent, skipIndent);

    /// <inheritdoc cref="Write(string, bool, bool)"/>
    private IndentingStringBuilder Write(ReadOnlySpan<char> content, string? originalString, bool splitContent, bool skipIndent = false)
    {
        if (splitContent)
        {
            while (content.Length > 0)
            {
                var endOfLineIndex = content.IndexOfAny(EndOfLineCharacters);
                if (endOfLineIndex >= 0)
                {
                    // consume through all the end-of-line characters we have.
                    while (endOfLineIndex < content.Length && IsEndOfLineCharacter(content[endOfLineIndex]))
                        endOfLineIndex++;

                    // Then write that content and sequence of end-of-lines to the buffer.
                    AppendSingleLine(content[0..endOfLineIndex], originalLine: null, skipIndent);

                    // Then start again with the content following the end of lines.
                    content = content[endOfLineIndex..];
                }
                else
                {
                    // no new line, append the rest of the content to the buffer.
                    AppendSingleLine(content, originalLine: null, skipIndent);
                    break;
                }
            }
        }
        else
        {
            AppendSingleLine(content, originalString, skipIndent);
        }

        return this;
    }

    /// <summary>
    /// Equivalent to <see cref="Write(string, bool, bool)"/> except that a final end of line sequence will be written after
    /// the content is written.
    /// </summary>
    public IndentingStringBuilder WriteLine(string content = "", bool splitContent = false, bool skipIndent = false)
        => WriteLine(condition: true, content.AsSpan(), content, splitContent, skipIndent);

    /// <inheritdoc cref="WriteLine(string, bool, bool)"/>
    public IndentingStringBuilder WriteLine(ReadOnlySpan<char> content, bool splitContent = false, bool skipIndent = false)
        => WriteLine(condition: true, content, originalContent: null, splitContent, skipIndent);

    public IndentingStringBuilder WriteLineIf(bool condition, string content = "", bool splitContent = false, bool skipIndent = false)
        => WriteLine(condition, content.AsSpan(), content, splitContent, skipIndent);

    /// <inheritdoc cref="WriteLine(string, bool, bool)"/>
    public IndentingStringBuilder WriteLineIf(bool condition, ReadOnlySpan<char> content, bool splitContent = false, bool skipIndent = false)
        => WriteLine(condition, content, originalContent: null, splitContent, skipIndent);

    /// <inheritdoc cref="WriteLine(string, bool, bool)"/>
    private IndentingStringBuilder WriteLine(bool condition, ReadOnlySpan<char> content, string? originalContent, bool splitContent = false, bool skipIndent = false)
    {
        if (condition)
        {
            Write(content, originalContent, splitContent, skipIndent);
            AppendEndOfLine();
        }

        return this;
    }

    /// <summary>
    /// Ensures that the current buffer has at least one blank line between the last written content and the content
    /// that would be written.  Note: a line containing only whitespace/indentation is not considered a blank line. Only
    /// a line with no content on it counts.
    /// </summary>
    /// <returns></returns>
    public IndentingStringBuilder EnsureBlankLine()
    {
        if (GetLineCount() < 2)
            AppendEndOfLine();

        return this;
    }

    private int GetLineCount()
    {
        var builder = this.Builder;
        var position = builder.Length - 1;
        var lineCount = 0;
        while (position >= 0)
        {
            if (builder[position] == '\n')
            {
                if (position >= 1 && builder[position - 1] == '\r')
                {
                    position -= 2;
                }
                else
                {
                    position--;
                }

                lineCount++;
            }
            else if (IsEndOfLineCharacter(builder[position]))
            {
                position--;
                lineCount++;
            }
            else
            {
                break;
            }
        }

        return lineCount;
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
    public Region EnterBlock()
        => EnterIndentedRegion("{", "}");

    public Region EnterIndentedRegion(string open = "", string close = "")
    {
        if (open != "")
            this.WriteLine(open);

        this.IncreaseIndent();
        return new Region(this, close);
    }

    public readonly struct Region(IndentingStringBuilder builder, string close) : IDisposable
    {
        public void Dispose()
        {
            builder.DecreaseIndent();

            if (close != "")
                builder.WriteLine(close);
        }
    }

#pragma warning disable IDE0060 // Remove unused parameter
    public IndentingStringBuilder Write(bool splitContent, [InterpolatedStringHandlerArgument("", nameof(splitContent))] WriteInterpolatedStringHandler handler)
        => this;

    public IndentingStringBuilder Write([InterpolatedStringHandlerArgument("")] WriteInterpolatedStringHandler handler)
        => this;

    public IndentingStringBuilder WriteIf(bool condition, bool splitContent, [InterpolatedStringHandlerArgument("", nameof(condition), nameof(splitContent))] WriteIfInterpolatedStringHandler handler)
        => this;

    public IndentingStringBuilder WriteIf(bool condition, [InterpolatedStringHandlerArgument("", nameof(condition))] WriteIfInterpolatedStringHandler handler)
        => this;

    public IndentingStringBuilder WriteLine(bool splitContent, [InterpolatedStringHandlerArgument("", nameof(splitContent))] WriteInterpolatedStringHandler handler)
    {
        Write(splitContent, handler);
        AppendEndOfLine();
        return this;
    }

    public IndentingStringBuilder WriteLine([InterpolatedStringHandlerArgument("")] WriteInterpolatedStringHandler handler)
    {
        Write(handler);
        AppendEndOfLine();
        return this;
    }

    public IndentingStringBuilder WriteLineIf(bool condition, bool splitContent, [InterpolatedStringHandlerArgument("", nameof(condition), nameof(splitContent))] WriteIfInterpolatedStringHandler handler)
    {
        if (condition)
            AppendEndOfLine();
        return this;
    }

    public IndentingStringBuilder WriteLineIf(bool condition, [InterpolatedStringHandlerArgument("", nameof(condition))] WriteIfInterpolatedStringHandler handler)
    {
        if (condition)
            AppendEndOfLine();
        return this;
    }

#pragma warning restore IDE0060 // Remove unused parameter

    public override string ToString()
        => this.Builder.ToString();

    /// <summary>
    /// Writes out the individual elements of <paramref name="content"/> ensuring that there is a blank line written
    /// between each element.
    /// </summary>
    public IndentingStringBuilder WriteBlankLineSeparated<T>(
        IEnumerable<T> content,
        Action<IndentingStringBuilder, T> writeElement)
    {
        return WriteBlankLineSeparated(
            content,
            static (builder, element, writeElement) => writeElement(builder, element),
            writeElement);
    }

    /// <inheritdoc cref="WriteBlankLineSeparated{T}(IEnumerable{T}, Action{IndentingStringBuilder, T})"/>
    public IndentingStringBuilder WriteBlankLineSeparated<T, TArg>(
        IEnumerable<T> content,
        Action<IndentingStringBuilder, T, TArg> writeElement,
        TArg arg)
    {
        return WriteSeparated(
            content,
            static (@this, _) => @this.EnsureBlankLine(),
            writeElement,
            arg, open: "", close: "");
    }

    public IndentingStringBuilder WriteCommaSeparated(
        IEnumerable<string> content, string open = "", string close = "")
    {
        return WriteCommaSeparated(
            content,
            static (builder, element) => builder.Write(element), open, close);
    }

    public IndentingStringBuilder WriteCommaSeparated<T>(
        IEnumerable<T> content,
        Action<IndentingStringBuilder, T> writeElement, string open = "", string close = "")
    {
        return WriteCommaSeparated(
            content,
            static (builder, element, writeElement) => writeElement(builder, element),
            writeElement, open, close);
    }

    public IndentingStringBuilder WriteCommaSeparated<T, TArg>(
        IEnumerable<T> content,
        Action<IndentingStringBuilder, T, TArg> writeElement,
        TArg arg, string open = "", string close = "")
    {
        return WriteSeparated(
            content,
            ", ",
            static (builder, element, tuple) => tuple.writeElement(builder, element, tuple.arg),
            (writeElement, arg), open, close);
    }

    public IndentingStringBuilder WriteSeparated<T>(
        IEnumerable<T> content,
        string separator,
        Action<IndentingStringBuilder, T> writeElement,
        string open = "", string close = "")
    {
        return WriteSeparated(
            content,
            separator,
            static (builder, element, writeElement) => writeElement(builder, element),
            writeElement,
            open, close);
    }

    public IndentingStringBuilder WriteSeparated<T, TArg>(
        IEnumerable<T> content,
        string separator,
        Action<IndentingStringBuilder, T, TArg> writeElement,
        TArg arg, string open = "", string close = "")
    {
        return WriteSeparated(
            content,
            static (@this, tuple) => @this.Write(tuple.separator),
            static (@this, item, tuple) => tuple.writeElement(@this, item, tuple.arg),
            (this, separator, writeElement, arg), open, close);
    }

    private IndentingStringBuilder WriteSeparated<T, TArg>(
        IEnumerable<T> content,
        Action<IndentingStringBuilder, TArg> writeSeparator,
        Action<IndentingStringBuilder, T, TArg> writeElement,
        TArg arg, string open, string close)
    {
        this.CheckDisposed();

        this.Write(open);

        var first = true;
        foreach (var item in content)
        {
            if (!first)
                writeSeparator(this, arg);

            var builderPosition = _builder.Length;
            writeElement(this, item, arg);

            // Only update us if writeElement actually wrote something.
            if (_builder.Length != builderPosition)
                first = false;
        }

        this.Write(close);

        return this;
    }

    //public SourceText ToSourceText(Encoding? encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
    //    => SourceText.From(new StringBuilderReader(this.Builder), this.Builder.Length, encoding, checksumAlgorithm);

    /// <summary>
    /// Provides a handler used by the language compiler to append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        private readonly IndentingStringBuilder _builder;
        private readonly bool _splitContent;

        public WriteInterpolatedStringHandler(int literalLength, int formattedCount, IndentingStringBuilder builder, bool splitContent = false)
        {
            _builder = builder;
            _splitContent = splitContent;
            // Assume that each formatted section adds at least one character.
            _builder.Builder.EnsureCapacity(_builder.Builder.Length + literalLength + formattedCount);
        }

        public void AppendLiteral(string literal)
            => _builder.Write(literal, _splitContent);

        public void AppendFormatted<T>(T value)
        {
            var str = value?.ToString();
            if (str is not null)
                _builder.Write(str, _splitContent);
        }

        public void AppendFormatted<T>(T value, string format) where T : IFormattable
        {
            var str = value?.ToString(format, formatProvider: null);
            if (str is not null)
                _builder.Write(str, _splitContent);
        }
    }

    /// <summary>
    /// Provides a handler used by the language compiler to append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteIfInterpolatedStringHandler
    {
        private readonly IndentingStringBuilder _builder;
        private readonly bool _condition;
        private readonly bool _splitContent;

        public WriteIfInterpolatedStringHandler(int literalLength, int formattedCount, IndentingStringBuilder builder, bool condition = true, bool splitContent = false)
        {
            _builder = builder;
            _condition = condition;
            _splitContent = splitContent;

            if (_condition)
                _builder.Builder.EnsureCapacity(_builder.Builder.Length + literalLength + formattedCount);
        }

        public void AppendLiteral(string literal)
        {
            if (_condition)
                _builder.Write(literal, _splitContent);
        }

        public void AppendFormatted<T>(T value)
        {
            if (_condition)
            {
                var str = value?.ToString();
                if (str is not null)
                    _builder.Write(str, _splitContent);
            }
        }

        public void AppendFormatted<T>(T value, string format) where T : IFormattable
        {
            if (_condition)
            {
                var str = value?.ToString(format, formatProvider: null);
                if (str is not null)
                    _builder.Write(str, _splitContent);
            }
        }
    }

    private sealed class StringBuilderReader : TextReader
    {
        private readonly StringBuilder _stringBuilder;
        private int _position;

        public StringBuilderReader(StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
            _position = 0;
        }

        public override int Peek()
            => _position == _stringBuilder.Length ? -1 : _stringBuilder[_position];

        public override int Read()
            => _position == _stringBuilder.Length ? -1 : _stringBuilder[_position++];

        public override int Read(char[] buffer, int index, int count)
        {
            var charsToCopy = Math.Min(count, _stringBuilder.Length - _position);
            _stringBuilder.CopyTo(_position, buffer, index, charsToCopy);
            _position += charsToCopy;
            return charsToCopy;
        }
    }
}
