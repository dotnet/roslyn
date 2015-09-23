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
        private struct Token
        {
            private readonly char _value;
            private readonly bool _isDelimiter;
            private readonly int _position;

            private Token(char value, int position, bool isDelimiter)
            {
                Debug.Assert(value != '\0');

                _value = value;
                _position = position;
                _isDelimiter = isDelimiter;
            }

            public int Position
            {
                get { return _position; }
            }

            public bool IsDelimiter
            {
                get { return _isDelimiter; }
            }

            public bool IsLiteral
            {
                get { return !_isDelimiter; }
            }

            public char Value
            {
                get { return _value; }
            }

            public TokenType TokenType
            {
                get
                {
                    if (IsDelimiter)
                        return (TokenType)_value;

                    return TokenType.Literal;
                }
            }

            public static Token Literal(char value, int position)
            {
                return new Token(value, position, isDelimiter: false);
            }

            public static Token Delimiter(char value, int position)
            {
                return new Token(value, position, isDelimiter: true);
            }
        }
    }
}
