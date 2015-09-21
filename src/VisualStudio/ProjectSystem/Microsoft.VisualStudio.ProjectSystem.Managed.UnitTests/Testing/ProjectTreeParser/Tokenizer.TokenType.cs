using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Testing
{
    internal partial class Tokenizer
    {
        internal enum TokenType
        {
            Literal = 0,
            LeftParenthesis = '(',
            RightParenthesis = ')',
            LeftBrace = '{',
            RightBrace = '}',
            Colon = ':',
            Comma = ',',
            WhiteSpace = ' ',
            Quote = '"',
            CarriageReturn = '\r',
        }
    }
}
