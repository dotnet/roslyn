// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class IntermediateNodeFormatter(
    StringBuilder builder,
    IntermediateNodeFormatter.FormatterMode mode = IntermediateNodeFormatter.FormatterMode.PreferContent,
    bool includeSource = false)
{
    // Depending on the usage of the formatter we might prefer thoroughness (properties)
    // or brevity (content). Generally if a node has a single string that provides value
    // it has content.
    //
    // Some nodes have neither: TagHelperBody
    // Some nodes have content: HtmlContent
    // Some nodes have properties: Document
    // Some nodes have both: TagHelperProperty
    public enum FormatterMode
    {
        PreferContent,
        PreferProperties,
    }

    private static readonly Dictionary<Type, string> s_nodeTypeToShortNameMap = [];

    private static readonly char[] s_charsToEscape = ['\r', '\n', '\t'];

    private readonly StringBuilder _builder = builder;
    private readonly FormatterMode Mode = mode;
    private readonly bool _includeSource = includeSource;

    private readonly Dictionary<string, string> _properties = new(StringComparer.Ordinal);
    private string? _content;

    private static string GetNodeShortName(IntermediateNode node)
    {
        lock (s_nodeTypeToShortNameMap)
        {
            return s_nodeTypeToShortNameMap.GetOrAdd(node.GetType(), ComputeShortName);
        }

        static string ComputeShortName(Type type)
        {
            const string ShortNameSuffix = nameof(IntermediateNode);

            var shortName = type.Name;

            if (shortName.EndsWith(ShortNameSuffix, StringComparison.Ordinal))
            {
                shortName = shortName[..^ShortNameSuffix.Length];
            }

            return shortName;
        }
    }

    public void WriteChildren(IntermediateNodeCollection children)
    {
        Debug.Assert(children != null);

        _builder.Append(" \"");

        foreach (var child in children)
        {
            if (child is IntermediateToken token)
            {
                WriteEscaped(token.Content);
            }
        }

        _builder.Append('"');
    }

    public void WriteContent(string? content)
    {
        if (content != null)
        {
            _content = content;
        }
    }

    public void WriteProperty(string key, string? value)
    {
        Debug.Assert(key != null);

        if (value != null)
        {
            _properties.Add(key, value);
        }
    }

    public void FormatNode(IntermediateNode node)
    {
        _builder.Append(GetNodeShortName(node));

        if (_includeSource)
        {
            _builder.Append(' ');
            _builder.Append(node.Source?.ToString() ?? "(n/a)");
        }

        node.FormatNode(this);

        if (ShouldWriteContent())
        {
            _builder.Append(" \"");
            WriteEscaped(_content);
            _builder.Append('"');
        }

        if (ShouldWriteProperties())
        {
            _builder.Append(" { ");

            var first = true;

            foreach (var (key, value) in _properties)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    _builder.Append(", ");
                }

                _builder.Append(key);
                _builder.Append(": \"");
                WriteEscaped(value);
                _builder.Append('"');
            }

            _builder.Append(" }");
        }

        _content = null;
        _properties.Clear();
    }

    [MemberNotNullWhen(true, nameof(_content))]
    private bool ShouldWriteContent()
        => _content != null && (_properties.Count == 0 || Mode == FormatterMode.PreferContent);

    private bool ShouldWriteProperties()
        => _properties.Count > 0 && (_content == null || Mode == FormatterMode.PreferContent);

    public void FormatTree(IntermediateNode node)
    {
        var visitor = new FormatterVisitor(this);
        visitor.Visit(node);
    }

    private void Write(char value, int repeatCount)
    {
        _builder.Append(value, repeatCount);
    }

    private void WriteLine()
    {
        _builder.AppendLine();
    }

    private void WriteEscaped(string content)
    {
        var startIndex = 0;
        int charToEscapeIndex;

        while ((charToEscapeIndex = content.IndexOfAny(s_charsToEscape, startIndex)) >= 0)
        {
            if (startIndex < charToEscapeIndex)
            {
                _builder.Append(content, startIndex, charToEscapeIndex - startIndex);
            }

            switch (content[charToEscapeIndex])
            {
                case '\r':
                    _builder.Append("\\r");
                    break;
                case '\n':
                    _builder.Append("\\n");
                    break;
                case '\t':
                    _builder.Append("\\t");
                    break;
            }

            startIndex = charToEscapeIndex + 1;
        }

        if (startIndex < content.Length)
        {
            _builder.Append(content, startIndex, content.Length - startIndex);
        }
    }

    private sealed class FormatterVisitor(IntermediateNodeFormatter formatter) : IntermediateNodeWalker
    {
        private const int IndentSize = 2;

        private readonly IntermediateNodeFormatter _formatter = formatter;
        private int _indent;

        public override void VisitDefault(IntermediateNode node)
        {
            // Indent
            _formatter.Write(' ', repeatCount: _indent);

            _formatter.FormatNode(node);
            _formatter.WriteLine();

            // Process children
            _indent += IndentSize;
            base.VisitDefault(node);
            _indent -= IndentSize;
        }
    }
}
