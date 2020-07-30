// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Text.Analyzers
{
    /// <summary>
    /// Source for "recognized" misspellings and "unrecognized" spellings obtained by parsing either
    /// XML or DIC code analysis dictionaries.
    /// </summary>
    /// <Remarks>
    /// <seealso href="https://docs.microsoft.com/en-us/visualstudio/code-quality/how-to-customize-the-code-analysis-dictionary?view=vs-2019"/>
    /// </Remarks>
    internal class CodeAnalysisDictionary
    {
        protected CodeAnalysisDictionary(XDocument document)
        {
            LoadWordsFromDocument(document);
        }

        protected CodeAnalysisDictionary(IEnumerable<string> recognizedWords)
        {
            RecognizedWords.UnionWith(recognizedWords);
        }

        /// <summary>
        /// Copy constructor used to implement <see cref="Clone"/>.
        /// </summary>
        /// <param name="other">The other instance whose values we should copy.</param>
        protected CodeAnalysisDictionary(CodeAnalysisDictionary other)
        {
            RecognizedWords = new HashSet<string>(other.RecognizedWords, StringComparer.OrdinalIgnoreCase);
            UnrecognizedWords = new HashSet<string>(other.UnrecognizedWords, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A list of misspelled words that the spell checker will now ignore.
        /// </summary>
        /// <example>
        /// <code>
        /// <Recognized>
        ///     <Word>knokker</Word>
        /// </Recognized>
        /// </code>
        /// </example>
        public HashSet<string> RecognizedWords { get; protected set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A list of correctly spelled words that the spell checker will now report.
        /// </summary>
        /// <example>
        /// <code>
        /// <Unrecognized>
        ///     <Word>meth</Word>
        /// </Unrecognized>
        /// </code>
        /// </example>
        public HashSet<string> UnrecognizedWords { get; protected set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new instance of this class with recognized and unrecognized words (if specified) loaded
        /// from the specified XML <paramref name="streamReader"/>.
        /// </summary>
        /// <param name="streamReader">XML stream of a code analysis dictionary.</param>
        /// <returns>A new instance of this class with the words loaded.</returns>
        public static CodeAnalysisDictionary CreateFromXml(StreamReader streamReader)
        {
            var document = XDocument.Load(streamReader);
            return new CodeAnalysisDictionary(document);
        }

        /// <summary>
        /// Creates a new instance of this class with recognized words loaded from the specified DIC <paramref name="streamReader"/>.
        /// </summary>
        /// <param name="streamReader">DIC stream of a code analysis dictionary.</param>
        /// <returns>A new instance of this class with recognized words loaded.</returns>
        public static CodeAnalysisDictionary CreateFromDic(StreamReader streamReader)
        {
            var words = new Collection<string>();

            string word;
            while ((word = streamReader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(word.Trim()))
                {
                    words.Add(word);
                }
            }

            return new CodeAnalysisDictionary(words);
        }

        /// <summary>
        /// Returns a copy of the current instance.
        /// </summary>
        /// <returns></returns>
        public CodeAnalysisDictionary Clone() => new CodeAnalysisDictionary(this);

        /// <summary>
        /// Replaces this instance's <see cref="RecognizedWords"/> and <see cref="UnrecognizedWords"/> with
        /// the union of its words and <paramref name="other"/>'s words.
        /// </summary>
        /// <param name="other">Another instance of this class.</param>
        /// <returns>This instance with words added from the other instance.</returns>
        public CodeAnalysisDictionary CombineWith(CodeAnalysisDictionary other)
        {
            RecognizedWords.UnionWith(other.RecognizedWords);
            UnrecognizedWords.UnionWith(other.UnrecognizedWords);
            return this;
        }

        private static IEnumerable<T> GetValues<T>(IEnumerable<XElement> elements, Func<XElement, T> extractor)
            => elements.Select(extractor);

        private static IEnumerable<XElement> GetElements(XDocument document, string section, string name)
            => document.Descendants(section).SelectMany(x => x.Elements(name));

        private static string ExtractInnerText(XElement element) => element.Value.Trim();

        private void LoadWordsFromDocument(XDocument document)
        {
            RecognizedWords.UnionWith(GetWordsOrExceptions("Recognized", "Word"));
            UnrecognizedWords.UnionWith(GetWordsOrExceptions("Unrecognized", "Word"));

            IEnumerable<string> GetWordsOrExceptions(string section, string property) =>
                GetValues(GetElements(document, section, property), ExtractInnerText);
        }
    }
}
