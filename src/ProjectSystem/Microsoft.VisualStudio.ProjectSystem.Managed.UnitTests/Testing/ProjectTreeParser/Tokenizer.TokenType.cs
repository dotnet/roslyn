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
            NewLine = '\n',
        }
    }
}
