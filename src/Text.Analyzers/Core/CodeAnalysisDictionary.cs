// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    internal sealed class CodeAnalysisDictionary
    {
        /// <summary>
        /// Initialize a new instance of <see cref="CodeAnalysisDictionary"/>.
        /// </summary>
        /// <param name="recognizedWords">Misspelled words that the spell checker will now ignore.</param>
        /// <param name="unrecognizedWords">Correctly spelled words that the spell checker will now report.</param>
        private CodeAnalysisDictionary(IEnumerable<string> recognizedWords, IEnumerable<string> unrecognizedWords)
        {
            RecognizedWords = new HashSet<string>(recognizedWords, StringComparer.OrdinalIgnoreCase);
            UnrecognizedWords = new HashSet<string>(unrecognizedWords, StringComparer.OrdinalIgnoreCase);
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
        public HashSet<string> RecognizedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        public HashSet<string> UnrecognizedWords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new instance of this class with recognized and unrecognized words (if specified) loaded
        /// from the specified XML <paramref name="streamReader"/>.
        /// </summary>
        /// <param name="streamReader">XML stream of a code analysis dictionary.</param>
        /// <returns>A new instance of this class with the words loaded.</returns>
        public static CodeAnalysisDictionary CreateFromXml(StreamReader streamReader)
        {
            var document = XDocument.Load(streamReader);
            // TODO: Include Deprecated words and acronym CasingExceptions as noted here:
            // https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1704?view=vs-2019#to-add-words-to-a-custom-dictionary
            return new CodeAnalysisDictionary(
                GetSectionWords(document, "Recognized", "Word"),
                GetSectionWords(document, "Unrecognized", "Word")
            );
        }

        /// <summary>
        /// Creates a new instance of this class with recognized words loaded from the specified DIC <paramref name="streamReader"/>.
        /// </summary>
        /// <param name="streamReader">DIC stream of a code analysis dictionary.</param>
        /// <returns>A new instance of this class with recognized words loaded.</returns>
        public static CodeAnalysisDictionary CreateFromDic(StreamReader streamReader)
        {
            var recognizedWords = new List<string>();

            string word;
            while ((word = streamReader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(word.Trim()))
                {
                    recognizedWords.Add(word);
                }
            }

            return new CodeAnalysisDictionary(recognizedWords, Enumerable.Empty<string>());
        }

        public static CodeAnalysisDictionary CreateFromDictionaries(IEnumerable<CodeAnalysisDictionary> dictionaries)
        {
            var recognizedWords = dictionaries.Select(x => x.RecognizedWords).Aggregate<IEnumerable<string>>((x, y) => x.Union(y));
            var unrecognizedWords = dictionaries.Select(x => x.UnrecognizedWords).Aggregate<IEnumerable<string>>((x, y) => x.Union(y));
            return new CodeAnalysisDictionary(recognizedWords, unrecognizedWords);
        }

        private static IEnumerable<string> GetSectionWords(XDocument document, string section, string property)
            => document.Descendants(section).SelectMany(section => section.Elements(property)).Select(element => element.Value.Trim());
    }
}
