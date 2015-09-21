using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Testing
{
    internal partial class Tokenizer
    {
        private readonly StringReader _reader;
        private readonly ImmutableArray<TokenType> _delimiters;

        public Tokenizer(StringReader reader, ImmutableArray<TokenType> delimiters)
        {
            Debug.Assert(reader != null);

            _reader = reader;
            _delimiters = delimiters;
        }

        public StringReader UnderlyingReader
        {
            get { return _reader; }
        }

        public TokenType? Peek(int lookAhead = 1)
        {
            Token token = PeekToken(lookAhead);
            if (token == null)
                return null;

            return token.TokenType;
        }

        public TokenType? PeekNext()
        {
            return Peek(2);
        }

        public void Skip(TokenType expected)
        {
            Token token = ReadToken();
            if (token == null)
            {
                throw new FormatException($"Expected '{(char)expected}' but encountered end of string");
            }

            if (token.TokenType != expected)
            {
                throw new FormatException($"Expected '{(char)expected}' but encountered '{token.Value}'");
            }
        }

        public bool SkipIf(TokenType expected)
        {
            if (Peek() != expected)
                return false;

            Skip(expected);
            return true;
        }

        public void Close()
        {
            Token token = ReadToken();
            if (token != null)
                throw new FormatException($"Expected end-of-string, but encountered '{token.Value}'");
        }

        public string ReadLiteral(LiteralParseOptions options)
        {
            string identifier = ReadLiteralCore(options);

            CheckIdentifier(identifier, options);

            identifier = identifier.TrimEnd((char)TokenType.WhiteSpace);
            CheckIdentifierAfterTrim(identifier, options);

            return identifier;
        }

        private string ReadLiteralCore(LiteralParseOptions options)
        {
            bool allowWhiteSpace = (options & LiteralParseOptions.AllowWhiteSpace) == LiteralParseOptions.AllowWhiteSpace;

            StringBuilder identifier = new StringBuilder();

            Token token;
            while ((token = PeekToken()) != null)
            {
                if (!IsIdentifier(token, allowWhiteSpace))
                    break;

                ReadToken();
                identifier.Append(token.Value);
            }

            return identifier.ToString();
        }

        private bool IsIdentifier(Token token, bool allowWhiteSpace)
        {
            if (allowWhiteSpace && token.IsWhiteSpace)
                return true;

            return token.IsLiteral;
        }

        private void CheckIdentifier(string identifier, LiteralParseOptions options)
        {
            if (IsValidIdentifier(identifier, options))
                return;

            // Are we at the end of the string?
            Token token = ReadToken();    // Consume token, so "position" is correct
            if (token == null)
            {
                throw new FormatException($"Expected identifier, but encountered end-of-string");
            }

            // Otherwise, we must have hit a delimiter as whitespace will have been consumed as part of the identifier
            throw new FormatException($"Expected identifier, but encountered '{token.Value}'");
        }

        private void CheckIdentifierAfterTrim(string identifier, LiteralParseOptions options)
        {
            if (!IsValidIdentifier(identifier, options))
                throw new FormatException("Expected identifier, but encountered only white space");
        }

        private bool IsValidIdentifier(string identifier, LiteralParseOptions options)
        {
            if ((options & LiteralParseOptions.Required) == LiteralParseOptions.Required)
            {
                return identifier.Length != 0;
            }

            return true;
        }

        private Token PeekToken()
        {
            return PeekToken(1);
        }

        private Token PeekToken(int lookAhead)
        {
            StringReader reader = _reader.Clone();

            Token token;
            while ((token = GetTokenFrom(reader)) != null)
            {
                lookAhead--;
                if (lookAhead == 0)
                    break;
            }

            return token;
        }

        private Token ReadToken()
        {
            return GetTokenFrom(_reader);
        }

        private Token GetTokenFrom(StringReader reader)
        {
            if (!reader.CanRead)
                return null;

            return GetToken(reader.Read());
        }

        private Token GetToken(char c)
        {
            if (IsDelimiter(c))
            {
                return Token.Delimiter(c);
            }

            if (IsWhiteSpace(c))
            {
                return Token.WhiteSpace(c);
            }

            // Otherwise, must be a literal
            return Token.Literal(c);
        }

        private bool IsWhiteSpace(char c)
        {
            return (TokenType)c == TokenType.WhiteSpace;
        }

        private bool IsDelimiter(char c)
        {
            return _delimiters.Contains((TokenType)c);
        }
    }
}
