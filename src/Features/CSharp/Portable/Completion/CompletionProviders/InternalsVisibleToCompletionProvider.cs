using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class InternalsVisibleToCompletionProvider : CommonCompletionProvider
    {
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
            var token = syntaxTree.FindTokenOnLeftOfPosition(context.Position, cancellationToken);
            var attr = token.GetAncestor<AttributeSyntax>();
            if (attr == null)
            {
                return;
            }
            if (attr.Name.TryGetNameParts(out IList<string> nameParts) && nameParts.Count > 0)
            {
                var lastName = nameParts[nameParts.Count - 1];
                if (lastName == "InternalsVisibleTo" || lastName == "InternalsVisibleToAttribute")
                {
                    foreach (var p in context.Document.Project.Solution.Projects)
                    {
                        var item = CompletionItem.Create(displayText: p.AssemblyName);
                        context.AddItem(item);
                    }
                }
            }
        }
    }
}
