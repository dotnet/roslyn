using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static System.Console;
using SymTable = System.Collections.Generic.HashSet<string>;
using Tokens = System.Collections.Generic.IEnumerable<MathsGenerator.Token>;

#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable RS1035 // Do not use banned APIs for analyzers

namespace MathsGenerator
{
    public enum TokenType
    {
        Number,
        Identifier,
        Operation,
        OpenParens,
        CloseParens,
        Equal,
        EOL,
        EOF,
        Spaces,
        Comma,
        Sum,
        None
    }

    public struct Token
    {
        public TokenType Type;
        public string Value;
        public int Line;
        public int Column;
    }

    public static class Lexer
    {

        public static void PrintTokens(IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                WriteLine($"{token.Line}, {token.Column}, {token.Type}, {token.Value}");
            }
        }

        static (TokenType, string)[] tokenStrings = {
            (TokenType.EOL,         @"(\r\n|\r|\n)"),
            (TokenType.Spaces,      @"\s+"),
            (TokenType.Number,      @"[+-]?((\d+\.?\d*)|(\.\d+))"),
            (TokenType.Identifier,  @"[_a-zA-Z][`'""_a-zA-Z0-9]*"),
            (TokenType.Operation,   @"[\+\-/\*]"),
            (TokenType.OpenParens,  @"[([{]"),
            (TokenType.CloseParens, @"[)\]}]"),
            (TokenType.Equal,       @"="),
            (TokenType.Comma,       @","),
            (TokenType.Sum,         @"∑")
        };

        static IEnumerable<(TokenType, Regex)> tokenExpressions =
            tokenStrings.Select(
                t => (t.Item1, new Regex($"^{t.Item2}", RegexOptions.Compiled | RegexOptions.Singleline)));

        // Can be optimized with spans to avoid so many allocations ...
        static public Tokens Tokenize(string source)
        {
            var currentLine = 1;
            var currentColumn = 1;

            while (source.Length > 0)
            {

                var matchLength = 0;
                var tokenType = TokenType.None;
                var value = "";

                foreach (var (type, rule) in tokenExpressions)
                {
                    var match = rule.Match(source);
                    if (match.Success)
                    {
                        matchLength = match.Length;
                        tokenType = type;
                        value = match.Value;
                        break;
                    }
                }

                if (matchLength == 0)
                {

                    throw new Exception($"Unrecognized symbol '{source[currentLine - 1]}' at index {currentLine - 1} (line {currentLine}, column {currentColumn}).");

                }
                else
                {

                    if (tokenType != TokenType.Spaces)
                        yield return new Token
                        {
                            Type = tokenType,
                            Value = value,
                            Line = currentLine,
                            Column = currentColumn
                        };

                    currentColumn += matchLength;
                    if (tokenType == TokenType.EOL)
                    {
                        currentLine += 1;
                        currentColumn = 0;
                    }

                    source = source.Substring(matchLength);
                }
            }

            yield return new Token
            {
                Type = TokenType.EOF,
                Line = currentLine,
                Column = currentColumn
            };
        }
    }

    /* EBNF for the language
        lines   = {line} EOF
        line    = {EOL} identifier [lround args rround] equal expr EOL {EOL}
        args    = identifier {comma identifier}
        expr    = [plus|minus] term { (plus|minus) term }
        term    = factor { (times|divide) factor };
        factor  = number | var | func | sum | matrix | lround expr rround;
        var     = identifier;
        func    = identifier lround expr {comma expr} rround;
        sum     = ∑ lround identifier comma expr comma expr comma expr rround;
    */
    public static class Parser
    {


        public static string Parse(Tokens tokens)
        {
            var globalSymbolTable = new SymTable();
            var symbolTable = new SymTable();
            var buffer = new StringBuilder();

            var en = tokens.GetEnumerator();
            en.MoveNext();

            buffer = Lines(new Context
            {
                tokens = en,
                globalSymbolTable = globalSymbolTable,
                symbolTable = symbolTable,
                buffer = buffer
            });
            return buffer.ToString();

        }

        private readonly static string Preamble = @"
using static System.Math;
using static Maths.FormulaHelpers;

namespace Maths {

    public static partial class Formulas { 
";
        private readonly static string Ending = @"
    }
}";

        private struct Context
        {
            public IEnumerator<Token> tokens;
            public SymTable globalSymbolTable;
            public SymTable symbolTable;
            public StringBuilder buffer;
        }

        private static StringBuilder Error(Token token, TokenType type, string value = "") =>
                throw new Exception($"Expected {type} {(value == "" ? "" : $" with {token.Value}")} at {token.Line},{token.Column} Instead found {token.Type} with value {token.Value}");

        static HashSet<string> validFunctions =
            new HashSet<string>(typeof(System.Math).GetMethods().Select(m => m.Name.ToLower()));

        static Dictionary<string, string> replacementStrings = new Dictionary<string, string> {
            {"'''", "Third" }, {"''", "Second" }, {"'", "Prime"}
        };

        private static StringBuilder EmitIdentifier(Context ctx, Token token)
        {
            var val = token.Value;

            if (val == "pi")
            {
                ctx.buffer.Append("PI"); // Doesn't follow pattern
                return ctx.buffer;
            }

            if (validFunctions.Contains(val))
            {
                ctx.buffer.Append(char.ToUpper(val[0]) + val.Substring(1));
                return ctx.buffer;
            }

            string id = token.Value;
            if (ctx.globalSymbolTable.Contains(token.Value) ||
                          ctx.symbolTable.Contains(token.Value))
            {
                foreach (var r in replacementStrings)
                {
                    id = id.Replace(r.Key, r.Value);
                }
                return ctx.buffer.Append(id);
            }
            else
            {
                throw new Exception($"{token.Value} not a known identifier or function.");
            }
        }

        private static StringBuilder Emit(Context ctx, Token token) => token.Type switch
        {
            TokenType.EOL => ctx.buffer.Append("\n"),
            TokenType.CloseParens => ctx.buffer.Append(')'), // All parens become rounded
            TokenType.OpenParens => ctx.buffer.Append('('),
            TokenType.Equal => ctx.buffer.Append("=>"),
            TokenType.Comma => ctx.buffer.Append(token.Value),

            // Identifiers are normalized and checked for injection attacks
            TokenType.Identifier => EmitIdentifier(ctx, token),
            TokenType.Number => ctx.buffer.Append(token.Value),
            TokenType.Operation => ctx.buffer.Append(token.Value),
            TokenType.Sum => ctx.buffer.Append("MySum"),
            _ => Error(token, TokenType.None)
        };

        private static bool Peek(Context ctx, TokenType type, string value = "")
        {
            var token = ctx.tokens.Current;

            return (token.Type == type && value == "") ||
               (token.Type == type && value == token.Value);
        }
        private static Token NextToken(Context ctx)
        {

            var token = ctx.tokens.Current;
            ctx.tokens.MoveNext();
            return token;
        }
        private static void Consume(Context ctx, TokenType type, string value = "")
        {

            var token = NextToken(ctx);

            if ((token.Type == type && value == "") ||
               (token.Type == type && value == token.Value))
            {

                ctx.buffer.Append(" ");
                Emit(ctx, token);
            }
            else
            {
                Error(token, type, value);
            }
        }

        private static StringBuilder Lines(Context ctx)
        {
            // lines    = {line} EOF

            ctx.buffer.Append(Preamble);

            while (!Peek(ctx, TokenType.EOF))
                Line(ctx);

            ctx.buffer.Append(Ending);

            return ctx.buffer;
        }

        private static void AddGlobalSymbol(Context ctx)
        {
            var token = ctx.tokens.Current;
            if (Peek(ctx, TokenType.Identifier))
            {
                ctx.globalSymbolTable.Add(token.Value);
            }
            else
            {
                Error(token, TokenType.Identifier);
            }
        }
        private static void AddSymbol(Context ctx)
        {
            var token = ctx.tokens.Current;
            if (Peek(ctx, TokenType.Identifier))
            {
                ctx.symbolTable.Add(token.Value);
            }
            else
            {
                Error(token, TokenType.Identifier);
            }
        }
        private static void Line(Context ctx)
        {
            // line    = {EOL} identifier [lround args rround] equal expr EOL {EOL}

            ctx.symbolTable.Clear();

            while (Peek(ctx, TokenType.EOL))
                Consume(ctx, TokenType.EOL);

            ctx.buffer.Append("\tpublic static double ");

            AddGlobalSymbol(ctx);
            Consume(ctx, TokenType.Identifier);

            if (Peek(ctx, TokenType.OpenParens, "("))
            {
                Consume(ctx, TokenType.OpenParens, "("); // Just round parens
                Args(ctx);
                Consume(ctx, TokenType.CloseParens, ")");
            }

            Consume(ctx, TokenType.Equal);
            Expr(ctx);
            ctx.buffer.Append(" ;");

            Consume(ctx, TokenType.EOL);

            while (Peek(ctx, TokenType.EOL))
                Consume(ctx, TokenType.EOL);
        }
        private static void Args(Context ctx)
        {
            // args    = identifier {comma identifier}
            // It doesn't make sense for a math function to have zero args (I think)

            ctx.buffer.Append("double ");
            AddSymbol(ctx);
            Consume(ctx, TokenType.Identifier);

            while (Peek(ctx, TokenType.Comma))
            {
                Consume(ctx, TokenType.Comma);
                ctx.buffer.Append("double ");
                AddSymbol(ctx);
                Consume(ctx, TokenType.Identifier);
            }
        }
        private static Func<Context, string, bool> IsOp = (ctx, op)
            => Peek(ctx, TokenType.Operation, op);
        private static Action<Context, string> ConsOp = (ctx, op)
            => Consume(ctx, TokenType.Operation, op);

        private static void Expr(Context ctx)
        {
            // expr    = [plus|minus] term { (plus|minus) term }

            if (IsOp(ctx, "+")) ConsOp(ctx, "+");
            if (IsOp(ctx, "-")) ConsOp(ctx, "-");

            Term(ctx);

            while (IsOp(ctx, "+") || IsOp(ctx, "-"))
            {

                if (IsOp(ctx, "+")) ConsOp(ctx, "+");
                if (IsOp(ctx, "-")) ConsOp(ctx, "-");

                Term(ctx);
            }
        }
        private static void Term(Context ctx)
        {
            // term    = factor { (times|divide) factor };
            Factor(ctx);

            while (IsOp(ctx, "*") || IsOp(ctx, "/"))
            {
                if (IsOp(ctx, "*")) ConsOp(ctx, "*");
                if (IsOp(ctx, "/")) ConsOp(ctx, "/");

                Term(ctx);
            }
        }
        private static void Factor(Context ctx)
        {
            // factor  = number | var | func | lround expr rround;
            if (Peek(ctx, TokenType.Number))
            {
                Consume(ctx, TokenType.Number);
                return;
            }

            if (Peek(ctx, TokenType.Identifier))
            {
                Consume(ctx, TokenType.Identifier); // Is either var or func
                if (Peek(ctx, TokenType.OpenParens, "("))
                { // Is Func, but we already consumed its name
                    Funct(ctx);
                }
                return;
            }
            if (Peek(ctx, TokenType.Sum))
            {
                Sum(ctx);
                return;
            }
            // Must be a parenthesized expression
            Consume(ctx, TokenType.OpenParens);
            Expr(ctx);
            Consume(ctx, TokenType.CloseParens);
        }
        private static void Sum(Context ctx)
        {
            // sum     = ∑ lround identifier comma expr1 comma expr2 comma expr3 rround;
            // TODO: differentiate in the language between integer and double, but complicated for a sample.
            Consume(ctx, TokenType.Sum);
            Consume(ctx, TokenType.OpenParens, "(");

            AddSymbol(ctx);
            var varName = NextToken(ctx).Value;
            NextToken(ctx); // consume the first comma without emitting it

            ctx.buffer.Append("(int)");
            Expr(ctx); // Start index
            Consume(ctx, TokenType.Comma);

            ctx.buffer.Append("(int)");
            Expr(ctx); // End index
            Consume(ctx, TokenType.Comma);

            ctx.buffer.Append($"{varName} => "); // It needs to be a lambda

            Expr(ctx); // expr to evaluate at each iteration

            Consume(ctx, TokenType.CloseParens, ")");
        }
        private static void Funct(Context ctx)
        {
            // func    = identifier lround expr {comma expr} rround;
            Consume(ctx, TokenType.OpenParens, "(");
            Expr(ctx);
            while (Peek(ctx, TokenType.Comma))
            {
                Consume(ctx, TokenType.Comma);
                Expr(ctx);
            }
            Consume(ctx, TokenType.CloseParens, ")");
        }
    }

    [Generator]
    public class MathsGenerator : ISourceGenerator
    {
        private const string libraryCode = @"
using System.Linq;
using System;
using System.Collections.Generic;

namespace Maths {
 public static class FormulaHelpers {

        public static IEnumerable<double> ConvertToDouble(IEnumerable<int> col)
        {
            foreach (var s in col)
                yield return (double) s;
        }

        public static double MySum(int start, int end, Func<double, double> f) =>
            Enumerable.Sum<double>(ConvertToDouble(Enumerable.Range(start, end - start)), f);
    }
}
";

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".math", StringComparison.OrdinalIgnoreCase))
                {
                    // Load formulas from .math files
                    var mathText = file.GetText();
                    var mathString = "";

                    if (mathText != null)
                    {
                        mathString = mathText.ToString();
                    }
                    else
                    {
                        throw new Exception($"Cannot load file {file.Path}");
                    }

                    // Get name of generated namespace from file name
                    string fileName = Path.GetFileNameWithoutExtension(file.Path);

                    // Parse and gen the formulas functions
                    var tokens = Lexer.Tokenize(mathString);
                    var code = Parser.Parse(tokens);

                    var codeFileName = $@"{fileName}.g.cs";

                    context.AddSource(codeFileName, SourceText.From(code, Encoding.UTF8));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((pi) => pi.AddSource("__MathLibrary__.g.cs", libraryCode));
        }
    }
}

