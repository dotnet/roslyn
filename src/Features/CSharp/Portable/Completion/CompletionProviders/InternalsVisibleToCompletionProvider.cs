using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : CommonCompletionProvider
    {
        private const string ProjectIdKey = "ProjectId";

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
                        AddAssemblyCompletionItems(context, cancellationToken);
                    }
                }
            }
        }

        private static AttributeSyntax GetAttributeSyntaxOfToken(SyntaxToken token)
        {
            //Supported cases:
            //[Attribute("|
            //[Attribute(parameterName:"Text|")
            //Also supported but excluded by syntaxTree.IsEntirelyWithinStringLiteral in ProvideCompletionsAsync
            //[Attribute(""|
            //[Attribute("Text"|)
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

        private static void AddAssemblyCompletionItems(CompletionContext context, CancellationToken cancellationToken)
        {
            var currentProject = context.Document.Project;
            foreach (var p in context.Document.Project.Solution.Projects)
            {
                if (p == currentProject)
                {
                    continue;
                }
                var completionItem = CompletionItem.Create(displayText: p.AssemblyName,
                    properties: ImmutableDictionary.Create<string, string>().Add(ProjectIdKey, p.Id.Id.ToString()));
                context.AddItem(completionItem);
            }
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default(char?), CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item.Properties.TryGetValue(ProjectIdKey, out var projectId))
            {
                var assemblyName = item.DisplayText;
                var project = document.Project.Solution.GetProject(ProjectId.CreateFromSerialized(new System.Guid(projectId)));
                var publicKey = await GetPublicKeyOfProjectAsync(project, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(publicKey))
                {
                    assemblyName += ", PublicKey=" + publicKey;
                }
                var tc = new TextChange(item.Span, assemblyName);
                return CompletionChange.Create(tc);
            }
            Debug.Fail("Project can't be found by projectId.");
            return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> GetPublicKeyOfProjectAsync(Project p, CancellationToken cancellationToken)
        {
            var compilationOptions = p.CompilationOptions;
            if (compilationOptions == null ||
                (string.IsNullOrEmpty(compilationOptions.CryptoKeyFile) && string.IsNullOrEmpty(compilationOptions.CryptoKeyContainer)))
            {
                return string.Empty;
            }
            var c = await p.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (c.Assembly?.Identity?.IsStrongName ?? false)
            {
                return GetPublicKeyAsHexString(c.Assembly.Identity.PublicKey);
            }
            return string.Empty;
        }

        private static string GetPublicKeyAsHexString(ImmutableArray<byte> publicKey)
        {
            var builder = SharedPools.Default<StringBuilder>().Allocate();
            try
            {
                builder.Clear();
                foreach (var b in publicKey)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
            finally
            {
                SharedPools.Default<StringBuilder>().ClearAndFree(builder);
            }
        }
    }
}
