using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : CommonCompletionProvider
    {
        private const string PublicKeyPropertyKey = "PublicKey";

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            var ch = text[insertedCharacterPosition];
            if (ch == '\"')
            {
                return true;
            }
            return base.IsInsertionTrigger(text, insertedCharacterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsEntirelyWithinStringLiteral(context.Position, cancellationToken))
            {
                var token = syntaxTree.FindTokenOnLeftOfPosition(context.Position, cancellationToken);
                var attr = GetAttributeSyntaxOfToken(token);
                if (attr == null)
                {
                    return;
                }
                if (attr.Name.TryGetNameParts(out IList<string> nameParts) && nameParts.Count > 0)
                {
                    var lastName = nameParts[nameParts.Count - 1];
                    if (lastName == "InternalsVisibleTo" || lastName == "InternalsVisibleToAttribute")
                    {
                        await AddAssemblyCompletionItems(context, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private static async Task AddAssemblyCompletionItems(CompletionContext context, CancellationToken cancellationToken)
        {
            var currentProject = context.Document.Project;
            foreach (var p in context.Document.Project.Solution.Projects)
            {
                if (p == currentProject)
                {
                    continue;
                }
                var publicKey = await GetPublicKeyOfProject(p, cancellationToken).ConfigureAwait(false);
                var item = string.IsNullOrEmpty(publicKey)
                    ? CompletionItem.Create(displayText: p.AssemblyName)
                    : CompletionItem.Create(displayText: p.AssemblyName, properties: ImmutableDictionary.Create<string, string>().Add(PublicKeyPropertyKey, publicKey));
                context.AddItem(item);
            }
        }

        private static async Task<string> GetPublicKeyOfProject(Project p, CancellationToken cancellationToken)
        {
            var compilationOptions = p.CompilationOptions;
            if (string.IsNullOrEmpty(compilationOptions.CryptoKeyFile) && string.IsNullOrEmpty(compilationOptions.CryptoKeyContainer))
            {
                return string.Empty;
            }
            var c = await p.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (c.Assembly?.Identity?.IsStrongName ?? false)
            {
                var sb = new StringBuilder(capacity: c.Assembly.Identity.PublicKey.Length * 2);
                foreach (var b in c.Assembly.Identity.PublicKey)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
            else
                return string.Empty;
        }

        private static AttributeSyntax GetAttributeSyntaxOfToken(SyntaxToken token)
        {
            var node = token.Parent;
            if (node is LiteralExpressionSyntax && node.Kind() == SyntaxKind.StringLiteralExpression)
            {
                node = node.Parent;
                if (node is AttributeArgumentSyntax)
                {
                    node = node.Parent;
                    if (node is AttributeArgumentListSyntax)
                    {
                        return node.Parent as AttributeSyntax;
                    }
                }
            }
            return default(AttributeSyntax);
        }

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            var assemblyName = selectedItem.DisplayText;
            if (selectedItem.Properties.TryGetValue(PublicKeyPropertyKey, out string publicKey))
                assemblyName += ", PublicKey=" + publicKey;
            TextChange? tc = new TextChange(selectedItem.Span, assemblyName);
            return Task.FromResult(tc);
        }
    }
}
