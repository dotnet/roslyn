using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Testing
{
    internal partial class Tokenizer
    {
        // Represents a self-contained unit within a tokenized string
        private class Token
        {
            private readonly char _value;
            private readonly TokenType _tokenType;

            private Token(char value, TokenType tokenType)
            {
                Debug.Assert(value != '\0');

                _value = value;
                _tokenType = tokenType;
            }

            public bool IsDelimiter
            {
                get
                {
                    switch (_tokenType)
                    {
                        case TokenType.LeftBrace:
                        case TokenType.RightBrace:
                        case TokenType.Comma:
                        case TokenType.Colon:
                        case TokenType.LeftParenthesis:
                        case TokenType.RightParenthesis:
                            return true;
                    }

                    Debug.Assert(IsWhiteSpace || IsLiteral);
                    return false;
                }
            }

            public bool IsWhiteSpace
            {
                get { return _tokenType == TokenType.WhiteSpace; }
            }

            public bool IsLiteral
            {
                get { return _tokenType == TokenType.Literal; }
            }

            public char Value
            {
                get { return _value; }
            }

            public TokenType TokenType
            {
                get { return _tokenType; }
            }

            public static Token Literal(char value)
            {
                return new Token(value, TokenType.Literal);
            }

            public static Token Delimiter(char value)
            {
                return new Token(value, (TokenType)value);
            }

            public static Token WhiteSpace(char value)
            {
                return new Token(value, TokenType.WhiteSpace);
            }
        }
    }
}
