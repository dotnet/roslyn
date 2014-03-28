// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in source code.
    /// </summary>
    [Serializable]
    internal sealed class SourceLocation : Location, IEquatable<SourceLocation>
    {
        private readonly SyntaxTree syntaxTree;
        private readonly TextSpan span;

        public SourceLocation(SyntaxTree syntaxTree, TextSpan span)
        {
            this.syntaxTree = syntaxTree;
            this.span = span;
        }

        public SourceLocation(SyntaxNode node)
            : this(node.SyntaxTree, node.Span)
        {
        }

        public SourceLocation(SyntaxToken token)
            : this(token.SyntaxTree, token.Span)
        {
        }

        public SourceLocation(SyntaxNodeOrToken nodeOrToken)
            : this(nodeOrToken.SyntaxTree, nodeOrToken.Span)
        {
        }

        public SourceLocation(SyntaxTrivia trivia)
            : this(trivia.SyntaxTree, trivia.Span)
        {
        }

        public SourceLocation(SyntaxReference syntaxRef)
            : this(syntaxRef.SyntaxTree, syntaxRef.Span)
        {
            // If we're using a syntaxref, we don't have a node in hand, so we couldn't get equality
            // on syntax node, so associatedNodeOpt shouldn't be set. We never use this constructor
            // when binding executable code anywhere, so it has no use.
        }

        public override string FilePath
        {
            get
            {
                return this.syntaxTree.FilePath;
            }
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.SourceFile;
            }
        }

        public override TextSpan SourceSpan
        {
            get
            {
                return span;
            }
        }

        public override SyntaxTree SourceTree
        {
            get
            {
                return syntaxTree;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            // If there's no syntax tree (e.g. because we're binding speculatively),
            // then just return an invalid span.
            if (syntaxTree == null)
            {
                FileLinePositionSpan result = default(FileLinePositionSpan);
                Debug.Assert(!result.IsValid);
                return result;
            }

            return syntaxTree.GetLineSpan(span);
        }

        public override FileLinePositionSpan GetMappedLineSpan()
        {
            // If there's no syntax tree (e.g. because we're binding speculatively),
            // then just return an invalid span.
            if (syntaxTree == null)
            {
                FileLinePositionSpan result = default(FileLinePositionSpan);
                Debug.Assert(!result.IsValid);
                return result;
            }

            return syntaxTree.GetMappedLineSpan(span);
        }

        public bool Equals(SourceLocation other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null && other.syntaxTree == syntaxTree && other.span == span;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SourceLocation);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(syntaxTree, span.GetHashCode());
        }

        protected override string GetDebuggerDisplay()
        {
            return base.GetDebuggerDisplay() + "\"" + syntaxTree.ToString().Substring(span.Start, span.Length) + "\"";
        }
    }
}