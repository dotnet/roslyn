// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    using JsonNodeOrToken = EmbeddedSyntaxNodeOrToken<JsonKind, JsonNode>;
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonSeparatedList = EmbeddedSeparatedSyntaxNodeList<JsonKind, JsonNode, JsonValueNode>;

    internal sealed class JsonCompilationUnit : JsonNode
    {
        public JsonCompilationUnit(ImmutableArray<JsonValueNode> sequence, JsonToken endOfFileToken)
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
        public ImmutableArray<JsonValueNode> Sequence { get; }
        public JsonToken EndOfFileToken { get; }

        internal override int ChildCount => Sequence.Length + 1;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            if (index == Sequence.Length)
                return EndOfFileToken;

            return Sequence[index];
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
    /// Represents a chunk of text that we did not understand as anything special.  i.e. it wasn't a keyword, number, or
    /// literal.  One common case of this is an unquoted property name (which json.net accepts).
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
            => index switch
            {
                0 => TextToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonObjectNode : JsonValueNode
    {
        public JsonObjectNode(
            JsonToken openBraceToken,
            JsonSeparatedList sequence,
            JsonToken closeBraceToken)
            : base(JsonKind.Object)
        {
            Debug.Assert(openBraceToken.Kind == JsonKind.OpenBraceToken);
            Debug.Assert(closeBraceToken.Kind == JsonKind.CloseBraceToken);

            OpenBraceToken = openBraceToken;
            Sequence = sequence;
            CloseBraceToken = closeBraceToken;
        }

        public JsonToken OpenBraceToken { get; }
        public JsonSeparatedList Sequence { get; }
        public JsonToken CloseBraceToken { get; }

        internal override int ChildCount => 2 + Sequence.NodesAndTokens.Length;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            if (index == 0)
                return OpenBraceToken;

            if (index == Sequence.NodesAndTokens.Length + 1)
                return CloseBraceToken;

            return Sequence.NodesAndTokens[index - 1];
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonArrayNode : JsonValueNode
    {
        public JsonArrayNode(
            JsonToken openBracketToken,
            ImmutableArray<JsonValueNode> sequence,
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
        public ImmutableArray<JsonValueNode> Sequence { get; }
        public JsonToken CloseBracketToken { get; }

        internal override int ChildCount => 2 + Sequence.Length;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            if (index == 0)
                return OpenBracketToken;

            if (index == Sequence.Length + 1)
                return CloseBracketToken;

            return Sequence[index - 1];
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonNegativeLiteralNode : JsonValueNode
    {
        public JsonNegativeLiteralNode(JsonToken minusToken, JsonToken literalToken)
            : base(JsonKind.NegativeLiteral)
        {
            Debug.Assert(minusToken.Kind == JsonKind.MinusToken);
            MinusToken = minusToken;
            LiteralToken = literalToken;
        }

        public JsonToken MinusToken { get; }
        public JsonToken LiteralToken { get; }

        internal override int ChildCount => 2;

        internal override JsonNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => MinusToken,
                1 => LiteralToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonLiteralNode(JsonToken literalToken) : JsonValueNode(JsonKind.Literal)
    {
        public JsonToken LiteralToken { get; } = literalToken;

        internal override int ChildCount => 1;

        internal override JsonNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => LiteralToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// See the note in <see cref="JsonParser"/> for why commas are stored as values for convenience.
    /// </summary>
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
            => index switch
            {
                0 => CommaToken,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    internal sealed class JsonPropertyNode : JsonValueNode
    {
        public JsonPropertyNode(JsonToken nameToken, JsonToken colonToken, JsonValueNode value)
            : base(JsonKind.Property)
        {
            // Note: the name is allowed by json.net to just be a text token, not a string.  e.g. `goo: 0` as opposed to
            // `"goo": 0`.  Strict json does not allow this.
            Debug.Assert(nameToken.Kind is JsonKind.StringToken or JsonKind.TextToken);
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
            => index switch
            {
                0 => NameToken,
                1 => ColonToken,
                2 => Value,
                _ => throw new InvalidOperationException(),
            };

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }

    /// <summary>
    /// Json.net construction.  It allows things like <c>new Date(1, 2, 3)</c>.  This is not allowed in strict mode.  
    /// </summary>
    internal sealed class JsonConstructorNode : JsonValueNode
    {
        public JsonConstructorNode(
            JsonToken newKeyword,
            JsonToken nameToken,
            JsonToken openParenToken,
            ImmutableArray<JsonValueNode> sequence,
            JsonToken closeParenToken)
            : base(JsonKind.Constructor)
        {
            Debug.Assert(newKeyword.Kind == JsonKind.NewKeyword);
            Debug.Assert(nameToken.Kind == JsonKind.TextToken);
            Debug.Assert(openParenToken.Kind == JsonKind.OpenParenToken);
            Debug.Assert(sequence != null);
            Debug.Assert(closeParenToken.Kind == JsonKind.CloseParenToken);
            NewKeyword = newKeyword;
            NameToken = nameToken;
            OpenParenToken = openParenToken;
            Sequence = sequence;
            CloseParenToken = closeParenToken;
        }

        public JsonToken NewKeyword { get; }
        public JsonToken NameToken { get; }
        public JsonToken OpenParenToken { get; }
        public ImmutableArray<JsonValueNode> Sequence { get; }
        public JsonToken CloseParenToken { get; }

        internal override int ChildCount => Sequence.Length + 4;

        internal override JsonNodeOrToken ChildAt(int index)
        {
            if (index == 0)
                return NewKeyword;

            if (index == 1)
                return NameToken;

            if (index == 2)
                return OpenParenToken;

            if (index == Sequence.Length + 3)
                return CloseParenToken;

            return Sequence[index - 3];
        }

        public override void Accept(IJsonNodeVisitor visitor)
            => visitor.Visit(this);
    }
}
