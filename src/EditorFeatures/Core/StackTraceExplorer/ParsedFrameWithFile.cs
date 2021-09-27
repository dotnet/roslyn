// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal class ParsedFrameWithFile : ParsedStackFrame
    {
        public TextSpan FileSpan { get; set; }

        public ParsedFrameWithFile(
            string originalLine,
            TextSpan classSpan,
            TextSpan methodSpan,
            TextSpan argsSpan,
            TextSpan fileSpan)
            : base(originalLine, classSpan, methodSpan, argsSpan)
        {
            Contract.Requires(fileSpan.Length > 0);
            FileSpan = fileSpan;
        }

        public string GetFileText()
        {
            return OriginalText[FileSpan.Start..FileSpan.End];
        }

        public string GetTextBetweenTypeAndFile()
        {
            var textBetweenLength = FileSpan.Start - ArgEndIndex;
            var textBetweenSpan = new TextSpan(ArgEndIndex, textBetweenLength);

            if (textBetweenSpan.Length > 0)
            {
                return OriginalText.Substring(textBetweenSpan.Start, textBetweenSpan.Length);
            }

            return string.Empty;
        }

        public override string GetTrailingText()
        {
            return OriginalText[FileSpan.End..];
        }

        internal (Document? document, int line) GetDocumentAndLine(Solution solution)
        {
            var fileMatches = GetFileMatches(solution, out var lineNumber);
            if (fileMatches.IsEmpty)
            {
                return (null, 0);
            }

            return (fileMatches.First(), lineNumber);
        }

        private ImmutableArray<Document> GetFileMatches(Solution solution, out int lineNumber)
        {
            var fileText = GetFileText();
            var regex = new Regex(@"(?<fileName>.+):(line)\s*(?<lineNumber>[0-9]+)");
            var match = regex.Match(fileText);
            Debug.Assert(match.Success);

            var fileNameGroup = match.Groups["fileName"];
            var lineNumberGroup = match.Groups["lineNumber"];

            lineNumber = int.Parse(lineNumberGroup.Value);

            var fileName = fileNameGroup.Value;
            Debug.Assert(!string.IsNullOrEmpty(fileName));

            var documentName = Path.GetFileName(fileName);
            var potentialMatches = new HashSet<Document>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == fileName)
                    {
                        return ImmutableArray.Create(document);
                    }

                    else if (document.Name == documentName)
                    {
                        potentialMatches.Add(document);
                    }
                }
            }

            return potentialMatches.ToImmutableArray();
        }
    }
}
