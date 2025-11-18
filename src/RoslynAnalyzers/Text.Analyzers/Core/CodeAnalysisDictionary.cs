// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Text.Analyzers
{
    /// <summary>
    /// Source for "recognized" misspellings and "unrecognized" spellings obtained by parsing either XML or DIC code
    /// analysis dictionaries.
    /// </summary>
    /// <Remarks>
    /// <seealso href="https://learn.microsoft.com/visualstudio/code-quality/how-to-customize-the-code-analysis-dictionary"/>
    /// </Remarks>
    internal sealed class CodeAnalysisDictionary
    {
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
        private readonly HashSet<string> _recognizedWords;

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
        private readonly HashSet<string> _unrecognizedWords;

        /// <summary>
        /// Initialize a new instance of <see cref="CodeAnalysisDictionary"/>.
        /// </summary>
        /// <param name="recognizedWords">Misspelled words that the spell checker will now ignore.</param>
        /// <param name="unrecognizedWords">Correctly spelled words that the spell checker will now report.</param>
        private CodeAnalysisDictionary(IEnumerable<string> recognizedWords, IEnumerable<string> unrecognizedWords)
        {
            _recognizedWords = new HashSet<string>(recognizedWords, StringComparer.OrdinalIgnoreCase);
            _unrecognizedWords = new HashSet<string>(unrecognizedWords, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a new instance of this class with recognized and unrecognized words (if specified) loaded
        /// from the specified XML <paramref name="streamReader"/>.
        /// </summary>
        /// <param name="streamReader">XML stream of a code analysis dictionary.</param>
        /// <returns>A new instance of this class with the words loaded.</returns>
        public static CodeAnalysisDictionary CreateFromXml(StreamReader streamReader)
        {
            var document = XDocument.Load(streamReader);
            // TODO: Include Deprecated/Compound terms as noted here:
            // https://learn.microsoft.com/visualstudio/code-quality/how-to-customize-the-code-analysis-dictionary
            // Tracked by:
            // https://github.com/dotnet/roslyn-analyzers/issues/4693
            return new CodeAnalysisDictionary(
                GetSectionWords(document, "Recognized", "Word"),
                GetSectionWords(document, "Unrecognized", "Word")
            );
        }

        /// <summary>
        /// Creates a new instance of this class with recognized words loaded from the specified DIC <paramref name="streamReader"/>.
        /// </summary>
        /// <remarks>
        /// A DIC file usually has an extension of ".dic". It consists of a list of newline-delimited words.
        /// </remarks>
        /// <param name="streamReader">DIC stream of a code analysis dictionary.</param>
        /// <returns>A new instance of this class with recognized words loaded.</returns>
        public static CodeAnalysisDictionary CreateFromDic(StreamReader streamReader)
        {
            var recognizedWords = new List<string>();

            string word;
            while ((word = streamReader.ReadLine()) != null)
            {
                var trimmedWord = word.Trim();
                if (trimmedWord.Length > 0)
                {
                    recognizedWords.Add(trimmedWord);
                }
            }

            return new CodeAnalysisDictionary(recognizedWords, Enumerable.Empty<string>());
        }

        private static IEnumerable<string> GetSectionWords(XDocument document, string section, string property)
            => document.Descendants(section).SelectMany(section => section.Elements(property)).Select(element => element.Value.Trim());

        public bool ContainsUnrecognizedWord(string word)
            => _unrecognizedWords.Contains(word);

        public bool ContainsRecognizedWord(string word)
            => _recognizedWords.Contains(word);
    }
}
