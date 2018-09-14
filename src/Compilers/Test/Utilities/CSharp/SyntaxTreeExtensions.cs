// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
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

        internal static string Dump(this SyntaxNode node)
        {
            var visitor = new CSharpSyntaxPrinter();
            visitor.Visit(node);
            return visitor.Dump();
        }

        internal static string Dump(this SyntaxTree tree)
        {
            return tree.GetRoot().Dump();
        }

        private class CSharpSyntaxPrinter : CSharpSyntaxWalker
        {
            PooledStringBuilder builder;
            int indent = 0;

            internal CSharpSyntaxPrinter()
            {
                builder = PooledStringBuilder.GetInstance();
            }

            internal string Dump()
            {
                return builder.ToStringAndFree();
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                builder.Builder.Append(' ', repeatCount: indent);
                builder.Builder.Append(node.Kind().ToString());
                if (node.IsMissing)
                {
                    builder.Builder.Append(" (missing)");
                }
                else if (node is IdentifierNameSyntax name)
                {
                    builder.Builder.Append(" ");
                    builder.Builder.Append(name.ToString());
                }
                builder.Builder.AppendLine();

                indent += 2;
                base.DefaultVisit(node);
                indent -= 2;
            }
        }
    }
}
