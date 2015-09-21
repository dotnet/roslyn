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
                throw new FormatException($"Expected '{expected}' but encountered end of string");
            }

            if (token.TokenType != expected)
            {
                throw new FormatException($"Expected '{expected}' but encountered '{token.Value}'");
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

        public string ReadId()
        {
            string identifier = ReadIdCore();

            CheckIdentifier(identifier);

            identifier = identifier.TrimEnd((char)TokenType.WhiteSpace);
            CheckIdentifierAfterTrim(identifier);

            return identifier;
        }

        private string ReadIdCore()
        {
            // TODO: Use a string builder cache
            StringBuilder identifier = new StringBuilder();

            Token token;
            while ((token = PeekToken(ParseOptions.IncludeWhiteSpace)) != null)
            {
                if (!token.IsIdentifier)
                    break;

                ReadToken(ParseOptions.IncludeWhiteSpace);
                identifier.Append(token.Value);
            }

            return identifier.ToString();
        }

        private void CheckIdentifier(string identifier)
        {
            if (IsValidIdentifier(identifier))
                return;

            // Are we at the end of the string?
            Token token = ReadToken(ParseOptions.IncludeWhiteSpace);    // Consume token, so "position" is correct
            if (token == null)
            {
                throw new FormatException($"Expected identifier, but encountered end-of-string");
            }

            // Otherwise, we must have hit a delimiter as whitespace will have been consumed as part of the identifier
            throw new FormatException($"Expected identifier, but encountered '{token.Value}'");
        }

        private void CheckIdentifierAfterTrim(string identifier)
        {
            if (!IsValidIdentifier(identifier))
                throw new FormatException("Expected identifier, but encountered only whitepsace");
        }

        private bool IsValidIdentifier(string identifier)
        {
            return identifier.Length != 0;
        }

        private Token PeekToken(ParseOptions options = ParseOptions.None)
        {
            return PeekToken(1, options);
        }

        private Token PeekToken(int lookAhead, ParseOptions options = ParseOptions.None)
        {
            StringReader reader = _reader.Clone();

            Token token;
            while ((token = ParseTokenFrom(reader, options)) != null)
            {
                lookAhead--;
                if (lookAhead == 0)
                    break;
            }

            return token;
        }

        private Token ReadToken(ParseOptions options = ParseOptions.None)
        {
            return ParseTokenFrom(_reader, options);
        }

        private Token ParseTokenFrom(StringReader reader, ParseOptions options)
        {
            Token token = GetTokenFrom(reader);
            if (token == null)
                return null;

            if (options != ParseOptions.IncludeWhiteSpace)
            {
                token = HandleWhiteSpace(token, reader);
            }

            return token;
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

        private Token HandleWhiteSpace(Token token, StringReader reader)
        {
            while (token != null && token.IsWhiteSpace)
            {
                token = GetTokenFrom(reader);
            }

            return token;
        }

    }
}
