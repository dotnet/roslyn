// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json
{
    using JsonNodeOrToken = EmbeddedSyntaxNodeOrToken<JsonKind, JsonNode>;
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;

    internal sealed class JsonCompilationUnit : JsonNode
    {
        public JsonCompilationUnit(JsonSequenceNode sequence, JsonToken endOfFileToken)
            : base(JsonKind.CompilationUnit)
        {
            Debug.Assert(sequence != null);
            Debug.Assert(endOfFileToken.Kind == JsonKind.EndOfFile);
            Sequence = sequence;
            EndOfFileToken = endOfFileToken;
        }

        /// <summary>
        /// For error recovery purposes, we support a sequence of nodes at the top level (even
        /// though only a single node is actually allowed).
        /// </summary>
        public JsonSequenceNode Sequence { get; }
        public JsonToken EndOfFileToken { get; }

        internal override int ChildCount => 2;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return Sequence;
                case 1: return EndOfFileToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Root of all value nodes.
    /// </summary>
    internal abstract class JsonValueNode : JsonNode
    {
        protected JsonValueNode(JsonKind kind)
            : base(kind)
        {
        }
    }

    /// <summary>
    /// Represents a possibly-empty sequence of json expressions.
    /// </summary>
    internal sealed class JsonSequenceNode : JsonNode
    {
        public ImmutableArray<JsonValueNode> Children { get; }

        internal override int ChildCount => Children.Length;

        public JsonSequenceNode(ImmutableArray<JsonValueNode> children)
            : base(JsonKind.Sequence)
        {
            Debug.Assert(children.All(v => v != null));
            this.Children = children;
        }

        public new JsonValueNode this[int index]
            => Children[index];

        internal override JsonNodeOrToken ChildAt(int index)
            => Children[index];

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Represents a chunk of text that we did not understand as anything special.  i.e. it wasn't a
    /// keyword, number, or literal.
    /// </summary>
    internal sealed class JsonTextNode : JsonValueNode
    {
        public JsonTextNode(JsonToken textToken)
            : base(JsonKind.Text)
        {
            Debug.Assert(textToken.Kind == JsonKind.TextToken);
            TextToken = textToken;
        }

        public JsonToken TextToken { get; }

        internal override int ChildCount => 1;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return TextToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonObjectNode : JsonValueNode
    {
        public JsonObjectNode(
            JsonToken openBraceToken,
            JsonSequenceNode sequence,
            JsonToken closeBraceToken)
            : base(JsonKind.Object)
        {
            Debug.Assert(openBraceToken.Kind == JsonKind.OpenBraceToken);
            Debug.Assert(sequence != null);
            Debug.Assert(closeBraceToken.Kind == JsonKind.CloseBraceToken);

            OpenBraceToken = openBraceToken;
            Sequence = sequence;
            CloseBraceToken = closeBraceToken;
        }

        public JsonToken OpenBraceToken { get; }
        public JsonSequenceNode Sequence { get; }
        public JsonToken CloseBraceToken { get; }

        internal override int ChildCount => 3;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return OpenBraceToken;
                case 1: return Sequence;
                case 2: return CloseBraceToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonArrayNode : JsonValueNode
    {
        public JsonArrayNode(
            JsonToken openBracketToken,
            JsonSequenceNode sequence,
            JsonToken closeBracketToken)
            : base(JsonKind.Array)
        {
            Debug.Assert(openBracketToken.Kind == JsonKind.OpenBracketToken);
            Debug.Assert(sequence != null);
            Debug.Assert(closeBracketToken.Kind == JsonKind.CloseBracketToken);

            OpenBracketToken = openBracketToken;
            Sequence = sequence;
            CloseBracketToken = closeBracketToken;
        }

        public JsonToken OpenBracketToken { get; }
        public JsonSequenceNode Sequence { get; }
        public JsonToken CloseBracketToken { get; }

        internal override int ChildCount => 3;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return OpenBracketToken;
                case 1: return Sequence;
                case 2: return CloseBracketToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonNegativeLiteralNode : JsonValueNode
    {
        public JsonNegativeLiteralNode(JsonToken minusToken, JsonToken literalToken)
            : base(JsonKind.NegativeLiteral)
        {
            MinusToken = minusToken;
            LiteralToken = literalToken;
        }

        public JsonToken MinusToken { get; }
        public JsonToken LiteralToken { get; }

        internal override int ChildCount => 2;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return MinusToken;
                case 1: return LiteralToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonLiteralNode : JsonValueNode
    {
        public JsonLiteralNode(JsonToken literalToken)
            : base(JsonKind.Literal)
        {
            LiteralToken = literalToken;
        }

        public JsonToken LiteralToken { get; }

        internal override int ChildCount => 1;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return LiteralToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonCommaValueNode : JsonValueNode
    {
        public JsonCommaValueNode(JsonToken commaToken)
            : base(JsonKind.CommaValue)
        {
            Debug.Assert(commaToken.Kind == JsonKind.CommaToken);
            CommaToken = commaToken;
        }

        public JsonToken CommaToken { get; }

        internal override int ChildCount => 1;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return CommaToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonPropertyNode : JsonValueNode
    {
        public JsonPropertyNode(JsonToken nameToken, JsonToken colonToken, JsonValueNode value)
            : base(JsonKind.Property)
        {
            Debug.Assert(nameToken.Kind == JsonKind.StringToken || nameToken.Kind == JsonKind.TextToken);
            Debug.Assert(colonToken.Kind == JsonKind.ColonToken);
            Debug.Assert(value != null);
            NameToken = nameToken;
            ColonToken = colonToken;
            Value = value;
        }

        public JsonToken NameToken { get; }
        public JsonToken ColonToken { get; }
        public JsonValueNode Value { get; }

        internal override int ChildCount => 3;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return NameToken;
                case 1: return ColonToken;
                case 2: return Value;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonConstructorNode : JsonValueNode
    {
        public JsonConstructorNode(
            JsonToken newKeyword, JsonToken nameToken, JsonToken openParenToken, JsonSequenceNode sequence, JsonToken closeParenToken)
            : base(JsonKind.Constructor)
        {
            NewKeyword = newKeyword;
            NameToken = nameToken;
            OpenParenToken = openParenToken;
            Sequence = sequence;
            CloseParenToken = closeParenToken;
        }

        public JsonToken NewKeyword { get; }
        public JsonToken NameToken { get; }
        public JsonToken OpenParenToken { get; }
        public JsonSequenceNode Sequence { get; }
        public JsonToken CloseParenToken { get; }

        internal override int ChildCount => 5;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            switch (index)
            {
                case 0: return NewKeyword;
                case 1: return NameToken;
                case 2: return OpenParenToken;
                case 3: return Sequence;
                case 4: return CloseParenToken;
            }

            throw new InvalidOperationException();
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }
}
