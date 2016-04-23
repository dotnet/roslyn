// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

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

        public TokenType? Peek()
        {
            Token? token = PeekToken();
            if (token == null)
                return null;

            return token.Value.TokenType;
        }

        public void Skip(TokenType expected)
        {
            Token? token = ReadToken();
            if (token == null)
            {
                throw FormatException(ProjectTreeFormatError.DelimiterExpected_EncounteredEndOfString, $"Expected '{(char)expected}' but encountered end of string.");
            }

            Token t = token.Value;

            if (t.TokenType != expected)
            {
                throw FormatException(ProjectTreeFormatError.DelimiterExpected, $"Expected '{(char)expected}' but encountered '{t.Value}'.");
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
            Token? token = ReadToken();
            if (token != null)
                throw FormatException(ProjectTreeFormatError.EndOfStringExpected, $"Expected end-of-string, but encountered '{token.Value.Value}'.");
        }

        public string ReadIdentifier(IdentifierParseOptions options)
        {
            string identifier = ReadIdentifierCore(options);

            CheckIdentifier(identifier, options);

            identifier = identifier.TrimEnd((char)TokenType.WhiteSpace);
            CheckIdentifierAfterTrim(identifier, options);

            return identifier;
        }

        private string ReadIdentifierCore(IdentifierParseOptions options)
        {
            StringBuilder identifier = new StringBuilder();

            Token? token;
            while ((token = PeekToken()) != null)
            {
                Token t = token.Value;

                if (t.IsDelimiter)
                    break;

                ReadToken();
                identifier.Append(t.Value);
            }

            return identifier.ToString();
        }

        private void CheckIdentifier(string identifier, IdentifierParseOptions options)
        {
            if (IsValidIdentifier(identifier, options))
                return;

            // Are we at the end of the string?
            Token? token = ReadToken();    // Consume token, so "position" is correct
            if (token == null)
            {
                throw FormatException(ProjectTreeFormatError.IdExpected_EncounteredEndOfString, $"Expected identifier, but encountered end-of-string.");
            }

            // Otherwise, we must have hit a delimiter as whitespace will have been consumed as part of the identifier
            throw FormatException(ProjectTreeFormatError.IdExpected_EncounteredDelimiter, $"Expected identifier, but encountered '{token.Value.Value}'.");
        }

        private void CheckIdentifierAfterTrim(string identifier, IdentifierParseOptions options)
        {
            if (!IsValidIdentifier(identifier, options))
                throw FormatException(ProjectTreeFormatError.IdExpected_EncounteredOnlyWhiteSpace, "Expected identifier, but encountered only white space.");
        }

        private bool IsValidIdentifier(string identifier, IdentifierParseOptions options)
        {
            if ((options & IdentifierParseOptions.Required) == IdentifierParseOptions.Required)
            {
                return identifier.Length != 0;
            }

            return true;
        }

        private Token? PeekToken()
        {
            StringReader reader = _reader.Clone();

            return GetTokenFrom(reader);
        }

        private Token? ReadToken()
        {
            return GetTokenFrom(_reader);
        }

        private Token? GetTokenFrom(StringReader reader)
        {
            if (reader.CanRead)
            {
                return GetToken(reader.Read());
            }

            return null;
        }

        private Token GetToken(char c)
        {
            if (IsDelimiter(c))
            {
                return Token.Delimiter(c);
            }

            // Otherwise, must be a literal
            return Token.Literal(c);
        }

        private bool IsDelimiter(char c)
        {
            return _delimiters.Contains((TokenType)c);
        }

        internal FormatException FormatException(ProjectTreeFormatError errorId, string message)
        {
            return new ProjectTreeFormatException(message,
                                                  errorId);
        }
    }
}
