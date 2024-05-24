// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml;
using Microsoft.CodeAnalysis.PooledObjects;
using XmlNames = Roslyn.Utilities.DocumentationCommentXmlNames;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <summary>
/// A documentation comment derived from either source text or metadata.
/// </summary>
internal sealed class DocumentationComment
{
    /// <summary>
    /// True if an error occurred when parsing.
    /// </summary>
    public bool HadXmlParseError { get; private set; }

    /// <summary>
    /// The full XML text of this tag.
    /// </summary>
    public string FullXmlFragment { get; private set; }

    /// <summary>
    /// The text in the &lt;example&gt; tag. Null if no tag existed.
    /// </summary>
    public string? ExampleText { get; private set; }

    /// <summary>
    /// The text in the &lt;summary&gt; tag. Null if no tag existed.
    /// </summary>
    public string? SummaryText { get; private set; }

    /// <summary>
    /// The text in the &lt;returns&gt; tag. Null if no tag existed.
    /// </summary>
    public string? ReturnsText { get; private set; }

    /// <summary>
    /// The text in the &lt;value&gt; tag. Null if no tag existed.
    /// </summary>
    public string? ValueText { get; private set; }

    /// <summary>
    /// The text in the &lt;remarks&gt; tag. Null if no tag existed.
    /// </summary>
    public string? RemarksText { get; private set; }

    /// <summary>
    /// The names of items in &lt;param&gt; tags.
    /// </summary>
    public ImmutableArray<string> ParameterNames { get; private set; }

    /// <summary>
    /// The names of items in &lt;typeparam&gt; tags.
    /// </summary>
    public ImmutableArray<string> TypeParameterNames { get; private set; }

    /// <summary>
    /// The types of items in &lt;exception&gt; tags.
    /// </summary>
    public ImmutableArray<string> ExceptionTypes { get; private set; }

    /// <summary>
    /// The item named in the &lt;completionlist&gt; tag's cref attribute.
    /// Null if the tag or cref attribute didn't exist.
    /// </summary>
    public string? CompletionListCref { get; private set; }

    /// <summary>
    /// Used for <see cref="CommentBuilder.TrimEachLine"/> method, to prevent new allocation of string
    /// </summary>
    private static readonly string[] s_NewLineAsStringArray = ["\n"];

    private DocumentationComment(string fullXmlFragment)
    {
        FullXmlFragment = fullXmlFragment;

        ParameterNames = [];
        TypeParameterNames = [];
        ExceptionTypes = [];
    }

    /// <summary>
    /// Cache of the most recently parsed fragment and the resulting DocumentationComment
    /// </summary>
    private static volatile DocumentationComment? s_cacheLastXmlFragmentParse;

    /// <summary>
    /// Parses and constructs a <see cref="DocumentationComment" /> from the given fragment of XML.
    /// </summary>
    /// <param name="xml">The fragment of XML to parse.</param>
    /// <returns>A DocumentationComment instance.</returns>
    public static DocumentationComment FromXmlFragment(string xml)
    {
        var result = s_cacheLastXmlFragmentParse;
        if (result == null || result.FullXmlFragment != xml)
        {
            // Cache miss
            result = CommentBuilder.Parse(xml);
            s_cacheLastXmlFragmentParse = result;
        }

        return result;
    }

    /// <summary>
    /// Helper class for parsing XML doc comments. Encapsulates the state required during parsing.
    /// </summary>
    private class CommentBuilder
    {
        private readonly DocumentationComment _comment;
        private ImmutableArray<string>.Builder? _parameterNamesBuilder;
        private ImmutableArray<string>.Builder? _typeParameterNamesBuilder;
        private ImmutableArray<string>.Builder? _exceptionTypesBuilder;
        private Dictionary<string, ImmutableArray<string>.Builder>? _exceptionTextBuilders;

        /// <summary>
        /// Parse and construct a <see cref="DocumentationComment" /> from the given fragment of XML.
        /// </summary>
        /// <param name="xml">The fragment of XML to parse.</param>
        /// <returns>A DocumentationComment instance.</returns>
        public static DocumentationComment Parse(string xml)
        {
            try
            {
                return new CommentBuilder(xml).ParseInternal(xml);
            }
            catch (Exception)
            {
                // It would be nice if we only had to catch XmlException to handle invalid XML
                // while parsing doc comments. Unfortunately, other exceptions can also occur,
                // so we just catch them all. See Dev12 Bug 612456 for an example.
                return new DocumentationComment(xml) { HadXmlParseError = true };
            }
        }

        private CommentBuilder(string xml)
            => _comment = new DocumentationComment(xml);

        private DocumentationComment ParseInternal(string xml)
        {
            XmlFragmentParser.ParseFragment(xml, ParseCallback, this);

            if (_exceptionTextBuilders != null)
            {
                foreach (var typeAndBuilderPair in _exceptionTextBuilders)
                {
                    _comment._exceptionTexts.Add(typeAndBuilderPair.Key, typeAndBuilderPair.Value.AsImmutable());
                }
            }

            _comment.ParameterNames = _parameterNamesBuilder == null ? [] : _parameterNamesBuilder.ToImmutable();
            _comment.TypeParameterNames = _typeParameterNamesBuilder == null ? [] : _typeParameterNamesBuilder.ToImmutable();
            _comment.ExceptionTypes = _exceptionTypesBuilder == null ? [] : _exceptionTypesBuilder.ToImmutable();

            return _comment;
        }

        private static void ParseCallback(XmlReader reader, CommentBuilder builder)
            => builder.ParseCallback(reader);

        // Find the shortest whitespace prefix and trim it from all the lines
        // Before:
        //  <summary>
        //  Line1
        //  <code>
        //     Line2
        //   Line3
        //  </code>
        //  </summary>
        // After:
        //<summary>
        //Line1
        //<code>
        //   Line2
        // Line3
        //</code>
        //</summary>
        //
        // We preserve the formatting to let the AbstractDocumentationCommentFormattingService get the unmangled
        // <code> blocks.
        // AbstractDocumentationCommentFormattingService will normalize whitespace for non-code element later.
        private static string TrimEachLine(string text)
        {
            var lines = text.Split(s_NewLineAsStringArray, StringSplitOptions.RemoveEmptyEntries);

            var maxPrefix = int.MaxValue;
            foreach (var line in lines)
            {
                var firstNonWhitespaceOffset = line.GetFirstNonWhitespaceOffset();

                // Don't include all-whitespace lines in the calculation
                // They'll be completely trimmed
                if (firstNonWhitespaceOffset == null)
                    continue;

                // note: this code presumes all whitespace should be treated uniformly (for example that a tab and
                // a space are equivalent).  If that turns out to be an issue we will need to revise this to determine
                // an appropriate strategy for trimming here.
                maxPrefix = Math.Min(maxPrefix, firstNonWhitespaceOffset.Value);
            }

            if (maxPrefix == int.MaxValue)
                return string.Empty;

            using var _ = PooledStringBuilder.GetInstance(out var builder);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (i != 0)
                    builder.AppendLine();

                var trimmedLine = line.TrimEnd();
                if (trimmedLine.Length != 0)
                    builder.Append(trimmedLine, maxPrefix, trimmedLine.Length - maxPrefix);
            }

            return builder.ToString();
        }

        private void ParseCallback(XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var localName = reader.LocalName;
                if (XmlNames.ElementEquals(localName, XmlNames.ExampleElementName) && _comment.ExampleText == null)
                {
                    _comment.ExampleText = TrimEachLine(reader.ReadInnerXml());
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.SummaryElementName) && _comment.SummaryText == null)
                {
                    _comment.SummaryText = TrimEachLine(reader.ReadInnerXml());
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.ReturnsElementName) && _comment.ReturnsText == null)
                {
                    _comment.ReturnsText = TrimEachLine(reader.ReadInnerXml());
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.ValueElementName) && _comment.ValueText == null)
                {
                    _comment.ValueText = TrimEachLine(reader.ReadInnerXml());
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.RemarksElementName) && _comment.RemarksText == null)
                {
                    _comment.RemarksText = TrimEachLine(reader.ReadInnerXml());
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.ParameterElementName))
                {
                    var name = reader.GetAttribute(XmlNames.NameAttributeName);
                    var paramText = reader.ReadInnerXml();

                    if (!string.IsNullOrWhiteSpace(name) && !_comment._parameterTexts.ContainsKey(name))
                    {
                        (_parameterNamesBuilder ??= ImmutableArray.CreateBuilder<string>()).Add(name);
                        _comment._parameterTexts.Add(name, TrimEachLine(paramText));
                    }
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.TypeParameterElementName))
                {
                    var name = reader.GetAttribute(XmlNames.NameAttributeName);
                    var typeParamText = reader.ReadInnerXml();

                    if (!string.IsNullOrWhiteSpace(name) && !_comment._typeParameterTexts.ContainsKey(name))
                    {
                        (_typeParameterNamesBuilder ??= ImmutableArray.CreateBuilder<string>()).Add(name);
                        _comment._typeParameterTexts.Add(name, TrimEachLine(typeParamText));
                    }
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.ExceptionElementName))
                {
                    var type = reader.GetAttribute(XmlNames.CrefAttributeName);
                    var exceptionText = reader.ReadInnerXml();

                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        if (_exceptionTextBuilders == null || !_exceptionTextBuilders.ContainsKey(type))
                        {
                            (_exceptionTypesBuilder ??= ImmutableArray.CreateBuilder<string>()).Add(type);
                            (_exceptionTextBuilders ??= []).Add(type, ImmutableArray.CreateBuilder<string>());
                        }

                        _exceptionTextBuilders[type].Add(exceptionText);
                    }
                }
                else if (XmlNames.ElementEquals(localName, XmlNames.CompletionListElementName))
                {
                    var cref = reader.GetAttribute(XmlNames.CrefAttributeName);
                    if (!string.IsNullOrWhiteSpace(cref))
                    {
                        _comment.CompletionListCref = cref;
                    }

                    reader.ReadInnerXml();
                }
                else
                {
                    // This is an element we don't handle. Skip it.
                    reader.Read();
                }
            }
            else
            {
                // We came across something that isn't a start element, like a block of text.
                // Skip it.
                reader.Read();
            }
        }
    }

    private readonly Dictionary<string, string> _parameterTexts = [];
    private readonly Dictionary<string, string> _typeParameterTexts = [];
    private readonly Dictionary<string, ImmutableArray<string>> _exceptionTexts = [];

    /// <summary>
    /// Returns the text for a given parameter, or null if no documentation was given for the parameter.
    /// </summary>
    public string? GetParameterText(string parameterName)
    {
        _parameterTexts.TryGetValue(parameterName, out var text);
        return text;
    }

    public DocumentationComment? GetParameter(string parameterName)
    {
        if (!_parameterTexts.TryGetValue(parameterName, out var text))
            return null;

        return new DocumentationComment(text) { SummaryText = text };
    }

    /// <summary>
    /// Returns the text for a given type parameter, or null if no documentation was given for the type parameter.
    /// </summary>
    public string? GetTypeParameterText(string typeParameterName)
    {
        _typeParameterTexts.TryGetValue(typeParameterName, out var text);
        return text;
    }

    public DocumentationComment? GetTypeParameter(string parameterName)
    {
        if (!_typeParameterTexts.TryGetValue(parameterName, out var text))
            return null;

        return new DocumentationComment(text) { SummaryText = text };
    }

    /// <summary>
    /// Returns the texts for a given exception, or an empty <see cref="ImmutableArray"/> if no documentation was given for the exception.
    /// </summary>
    public ImmutableArray<string> GetExceptionTexts(string exceptionName)
    {
        _exceptionTexts.TryGetValue(exceptionName, out var texts);

        if (texts.IsDefault)
        {
            // If the exception wasn't found, TryGetValue will set "texts" to a default value.
            // To be friendly, we want to return an empty array rather than a null array.
            texts = [];
        }

        return texts;
    }

    /// <summary>
    /// An empty comment.
    /// </summary>
    public static readonly DocumentationComment Empty = new(string.Empty);
}
