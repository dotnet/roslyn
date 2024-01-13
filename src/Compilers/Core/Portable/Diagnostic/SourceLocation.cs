// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in source code.
    /// </summary>
    internal sealed class SourceLocation : Location, IEquatable<SourceLocation?>
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly TextSpan _span;

        public SourceLocation(SyntaxTree syntaxTree, TextSpan span)
        {
            _syntaxTree = syntaxTree;
            _span = span;
        }

        public SourceLocation(SyntaxNode node)
            : this(node.SyntaxTree, node.Span)
        {
        }

        public SourceLocation(in SyntaxToken token)
            : this(token.SyntaxTree!, token.Span)
        {
        }

        public SourceLocation(in SyntaxNodeOrToken nodeOrToken)
            : this(nodeOrToken.SyntaxTree!, nodeOrToken.Span)
        {
            Debug.Assert(nodeOrToken.SyntaxTree is object);
        }

        public SourceLocation(in SyntaxTrivia trivia)
            : this(trivia.SyntaxTree!, trivia.Span)
        {
            Debug.Assert(trivia.SyntaxTree is object);
        }

        public SourceLocation(SyntaxReference syntaxRef)
            : this(syntaxRef.SyntaxTree, syntaxRef.Span)
        {
            // If we're using a syntaxref, we don't have a node in hand, so we couldn't get equality
            // on syntax node, so associatedNodeOpt shouldn't be set. We never use this constructor
            // when binding executable code anywhere, so it has no use.
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
                return _span;
            }
        }

        public override SyntaxTree SourceTree
        {
            get
            {
                return _syntaxTree;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            // If there's no syntax tree (e.g. because we're binding speculatively),
            // then just return an invalid span.
            if (_syntaxTree == null)
            {
                FileLinePositionSpan result = default(FileLinePositionSpan);
                Debug.Assert(!result.IsValid);
                return result;
            }

            return _syntaxTree.GetLineSpan(_span);
        }

        public override FileLinePositionSpan GetMappedLineSpan()
        {
            // If there's no syntax tree (e.g. because we're binding speculatively),
            // then just return an invalid span.
            if (_syntaxTree == null)
            {
                FileLinePositionSpan result = default(FileLinePositionSpan);
                Debug.Assert(!result.IsValid);
                return result;
            }

            return _syntaxTree.GetMappedLineSpan(_span);
        }

        public bool Equals(SourceLocation? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null && other._syntaxTree == _syntaxTree && other._span == _span;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as SourceLocation);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_syntaxTree, _span.GetHashCode());
        }

        protected override string GetDebuggerDisplay()
        {
            return base.GetDebuggerDisplay() + "\"" + _syntaxTree.ToString().Substring(_span.Start, _span.Length) + "\"";
        }
    }
}
