using System.Linq;

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    internal static class SyntaxNodeArrayExtensions
    {
        public static SyntaxNode ConvertToTriviaList(this SyntaxNode[] nodes)
        {
            var builder = new SyntaxListBuilder(nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var nd = node.GetDiagnostics();
                foreach (var token in node.GetTokens())
                {
                    builder.Add(token.GetLeadingTrivia());
                    if (token.Width > 0)
                    {
                        var tk = token.WithLeadingTrivia(null).WithTrailingTrivia(null);
                        System.Diagnostics.Debug.Assert(tk.HasDiagnostics == token.HasDiagnostics);
                        if (nd != null && nd.Length > 0)
                        {
                            if (token != node)
                            {
                                tk = tk.WithAdditionalDiagnostics(nd);
                            }

                            nd = null;
                        }

                        builder.Add(Syntax.SkippedTokens(tk));
                    }

                    builder.Add(token.GetTrailingTrivia());
                }
            }

            return builder.ToListNode();
        }

#if false
        public static SyntaxNode ConvertToTriviaList(this SyntaxNode[] nodes)
        {
            var builder = new SyntaxListBuilder(nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var nd = node.GetDiagnostics();
                foreach (var token in node.GetTokens())
                {
                    builder.Add(token.GetLeadingTrivia());
                    if (token.Width > 0)
                    {
                        var text = Syntax.SkippedText(token.GetText());
                        var d = token.GetDiagnostics();
                        if (d.Length > 0)
                        {
                            text = text.WithDiagnostics(d);
                        }

                        if (nd != null && nd.Length > 0)
                        {
                            if (token != node)
                            {
                                text = text.WithAdditionalDiagnostics(nd);
                            }

                            nd = null;
                        }

                        builder.Add(text);
                    }

                    builder.Add(token.GetTrailingTrivia());
                }
            }

            return builder.ToListNode();
        }
#endif
    }
}