// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CodeFixes.NamingStyles
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.ApplyNamingStyle), Shared]
    internal class NamingStyleCodeFixProvider : CodeFixProvider
    {

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public NamingStyleCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.NamingRuleId);

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var model = await document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var serializedNamingStyle = diagnostic.Properties[nameof(NamingStyle)];
                var style = NamingStyle.FromXElement(XElement.Parse(serializedNamingStyle));

                var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                var node = root.FindNode(span);

                // it is usually the right one (such as a variable declarator, designation or a foreach statement)
                // because there is no other node in between. But there is one case in a VB catch clause where the token
                // is wrapped in an identifier name. So if what we found is an identifier, take the parent node instead.
                // Note that this is the correct thing to do because GetDeclaredSymbol never works on identifier names.
                if (syntaxFactsService.IsIdentifierName(node))
                    node = node.Parent;

                if (node is null)
                    continue;

                var symbol = model.GetDeclaredSymbol(node, context.CancellationToken);
                // TODO: We should always be able to find the symbol that generated this diagnostic,
                // but this cannot always be done by simply asking for the declared symbol on the node 
                // from the symbol's declaration location.
                // See https://github.com/dotnet/roslyn/issues/16588
                if (symbol is null)
                    continue;

                var fixedNames = style.MakeCompliant(symbol.Name);
                foreach (var fixedName in fixedNames)
                {
                    // NOTE:
                    // This depends on how https://github.com/dotnet/roslyn/pull/55033 would call IVsRefactorNotify correctly
                    var codeAction = new CustomCodeActions.SolutionChangeAction(
                        string.Format(CodeFixesResources.Fix_Name_Violation_colon_0, fixedName),
                        c => FixAsync(document, symbol, fixedName, c),
                        nameof(NamingStyleCodeFixProvider));

                    context.RegisterCodeFix(codeAction, diagnostic);
                }
            }
        }

        private static async Task<Solution> FixAsync(
            Document document, ISymbol symbol, string fixedName, CancellationToken cancellationToken)
        {
            return await Renamer.RenameSymbolAsync(
                document.Project.Solution, symbol, new SymbolRenameOptions(), fixedName,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
