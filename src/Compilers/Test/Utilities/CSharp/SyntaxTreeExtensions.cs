// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class SyntaxTreeExtensions
    {
        public static SyntaxTree WithReplace(this SyntaxTree syntaxTree, int offset, int length, string newText)
        {
            var oldFullText = syntaxTree.GetText();
            var newFullText = oldFullText.WithChanges(new TextChange(new TextSpan(offset, length), newText));
            return syntaxTree.WithChangedText(newFullText);
        }

        public static SyntaxTree WithReplaceFirst(this SyntaxTree syntaxTree, string oldText, string newText)
        {
            var oldFullText = syntaxTree.GetText().ToString();
            int offset = oldFullText.IndexOf(oldText, StringComparison.Ordinal);
            int length = oldText.Length;
            return WithReplace(syntaxTree, offset, length, newText);
        }

        public static SyntaxTree WithReplace(this SyntaxTree syntaxTree, int startIndex, string oldText, string newText)
        {
            var oldFullText = syntaxTree.GetText().ToString();
            int offset = oldFullText.IndexOf(oldText, startIndex, StringComparison.Ordinal); // Use an offset to find the first element to replace at
            int length = oldText.Length;
            return WithReplace(syntaxTree, offset, length, newText);
        }

        public static SyntaxTree WithInsertAt(this SyntaxTree syntaxTree, int offset, string newText)
        {
            return WithReplace(syntaxTree, offset, 0, newText);
        }

        public static SyntaxTree WithInsertBefore(this SyntaxTree syntaxTree, string existingText, string newText)
        {
            var oldFullText = syntaxTree.GetText().ToString();
            int offset = oldFullText.IndexOf(existingText, StringComparison.Ordinal);
            return WithReplace(syntaxTree, offset, 0, newText);
        }

        public static SyntaxTree WithRemoveAt(this SyntaxTree syntaxTree, int offset, int length)
        {
            return WithReplace(syntaxTree, offset, length, string.Empty);
        }

        public static SyntaxTree WithRemoveFirst(this SyntaxTree syntaxTree, string oldText)
        {
            return WithReplaceFirst(syntaxTree, oldText, string.Empty);
        }
    }
}
