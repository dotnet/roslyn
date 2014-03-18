// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Xml;
using XmlNames = Roslyn.Utilities.DocumentationCommentXmlNames;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
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
        public string ExampleText { get; private set; }

        /// <summary>
        /// The text in the &lt;summary&gt; tag. Null if no tag existed.
        /// </summary>
        public string SummaryText { get; private set; }

        /// <summary>
        /// The text in the &lt;returns&gt; tag. Null if no tag existed.
        /// </summary>
        public string ReturnsText { get; private set; }

        /// <summary>
        /// The text in the &lt;remarks&gt; tag. Null if no tag existed.
        /// </summary>
        public string RemarksText { get; private set; }

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

        private DocumentationComment()
        {
            ParameterNames = ImmutableArray.Create<string>();
            TypeParameterNames = ImmutableArray.Create<string>();
            ExceptionTypes = ImmutableArray.Create<string>();
        }

        /// <summary>
        /// Parses and constructs a <see cref="DocumentationComment" /> from the given fragment of XML.
        /// </summary>
        /// <param name="xml">The fragment of XML to parse.</param>
        /// <returns>A DocumentationComment instance.</returns>
        public static DocumentationComment FromXmlFragment(string xml)
        {
            try
            {
                // TODO: probably want to preserve whitespace (DevDiv #13045).
                XmlReader reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });

                DocumentationComment comment = new DocumentationComment();
                comment.FullXmlFragment = xml;

                List<string> parameterNamesBuilder = new List<string>();
                List<string> typeParameterNamesBuilder = new List<string>();
                List<string> exceptionTypesBuilder = new List<string>();

                try
                {
                    Dictionary<string, List<string>> exceptionTextBuilders = new Dictionary<string, List<string>>();

                    while (!reader.EOF)
                    {
                        if (reader.IsStartElement())
                        {
                            string localName = reader.LocalName;
                            if (XmlNames.ElementEquals(localName, XmlNames.ExampleElementName) && comment.ExampleText == null)
                            {
                                comment.ExampleText = reader.ReadInnerXml().Trim(); // TODO: trim each line
                            }
                            else if (XmlNames.ElementEquals(localName, XmlNames.SummaryElementName) && comment.SummaryText == null)
                            {
                                comment.SummaryText = reader.ReadInnerXml().Trim(); // TODO: trim each line
                            }
                            else if (XmlNames.ElementEquals(localName, XmlNames.ReturnsElementName) && comment.ReturnsText == null)
                            {
                                comment.ReturnsText = reader.ReadInnerXml().Trim(); // TODO: trim each line
                            }
                            else if (XmlNames.ElementEquals(localName, XmlNames.RemarksElementName) && comment.RemarksText == null)
                            {
                                comment.RemarksText = reader.ReadInnerXml().Trim(); // TODO: trim each line
                            }
                            else if (XmlNames.ElementEquals(localName, XmlNames.ParameterElementName))
                            {
                                string name = reader.GetAttribute(XmlNames.NameAttributeName);
                                string paramText = reader.ReadInnerXml();

                                if (!string.IsNullOrWhiteSpace(name) && !comment.parameterTexts.ContainsKey(name))
                                {
                                    parameterNamesBuilder.Add(name);
                                    comment.parameterTexts.Add(name, paramText.Trim()); // TODO: trim each line
                                }
                            }
                            else if (XmlNames.ElementEquals(localName, XmlNames.TypeParameterElementName))
                            {
                                string name = reader.GetAttribute(XmlNames.NameAttributeName);
                                string typeParamText = reader.ReadInnerXml();

                                if (!string.IsNullOrWhiteSpace(name) && !comment.typeParameterTexts.ContainsKey(name))
                                {
                                    typeParameterNamesBuilder.Add(name);
                                    comment.typeParameterTexts.Add(name, typeParamText.Trim()); // TODO: trim each line
                                }
                            }
                            else if (XmlNames.ElementEquals(localName, XmlNames.ExceptionElementName))
                            {
                                string type = reader.GetAttribute(XmlNames.CrefAttributeName);
                                string exceptionText = reader.ReadInnerXml();

                                if (!string.IsNullOrWhiteSpace(type))
                                {
                                    if (!exceptionTextBuilders.ContainsKey(type))
                                    {
                                        exceptionTypesBuilder.Add(type);
                                        exceptionTextBuilders.Add(type, new List<string>());
                                    }

                                    exceptionTextBuilders[type].Add(exceptionText);
                                }
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

                    foreach (var typeAndBuilderPair in exceptionTextBuilders)
                    {
                        comment.exceptionTexts.Add(typeAndBuilderPair.Key,
typeAndBuilderPair.Value.AsImmutable());
                    }
                }
                finally
                {
                    comment.ParameterNames = parameterNamesBuilder.AsImmutable();
                    comment.TypeParameterNames = typeParameterNamesBuilder.AsImmutable();
                    comment.ExceptionTypes = exceptionTypesBuilder.AsImmutable();
                }

                return comment;
            }
            catch (Exception)
            {
                // It would be nice if we only had to catch XmlException to handle invalid XML
                // while parsing doc comments. Unfortunately, other exceptions can also occur,
                // so we just catch them all. See Dev12 Bug 612456 for an example.
                return new DocumentationComment { FullXmlFragment = xml, HadXmlParseError = true };
            }
        }

        private readonly Dictionary<string, string> parameterTexts = new Dictionary<string, string>();
        private readonly Dictionary<string, string> typeParameterTexts = new Dictionary<string, string>();
        private readonly Dictionary<string, ImmutableArray<string>> exceptionTexts = new Dictionary<string, ImmutableArray<string>>();

        /// <summary>
        /// Returns the text for a given parameter, or null if no documentation was given for the parameter.
        /// </summary>
        public string GetParameterText(string parameterName)
        {
            string text;
            parameterTexts.TryGetValue(parameterName, out text);
            return text;
        }

        /// <summary>
        /// Returns the text for a given type parameter, or null if no documentation was given for the type parameter.
        /// </summary>
        /// <param name="typeParameterName"></param>
        /// <returns></returns>
        public string GetTypeParameterText(string typeParameterName)
        {
            string text;
            typeParameterTexts.TryGetValue(typeParameterName, out text);
            return text;
        }

        /// <summary>
        /// Returns the texts for a given exception, or an empty <see cref="ImmutableArray"/> if no documentation was given for the exception.
        /// </summary>
        public ImmutableArray<string> GetExceptionTexts(string exceptionName)
        {
            ImmutableArray<string> texts;
            exceptionTexts.TryGetValue(exceptionName, out texts);

            if (texts.IsDefault)
            {
                // If the exception wasn't found, TryGetValue will set "texts" to a default value.
                // To be friendly, we want to return an empty array rather than a null array.
                texts = ImmutableArray.Create<string>();
            }

            return texts;
        }

        /// <summary>
        /// An empty comment.
        /// </summary>
        public static readonly DocumentationComment Empty = new DocumentationComment();
    }
}
