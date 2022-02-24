// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Text;

#if !CODE_STYLE  // https://github.com/dotnet/roslyn/issues/42218 removing dependency on WorkspaceServices.
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
#endif

namespace Microsoft.CodeAnalysis.CodeFixes.NamingStyles
{
#if !CODE_STYLE  // https://github.com/dotnet/roslyn/issues/42218 tracks enabling this fixer in CodeStyle layer.
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.ApplyNamingStyle), Shared]
#endif
    internal partial class NamingStyleCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public NamingStyleCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.NamingRuleId);

        public override FixAllProvider? GetFixAllProvider() => NamingStyleCodeFixAllProvider.Instance;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var serializedNamingStyle = diagnostic.Properties[nameof(NamingStyle)];
            var style = NamingStyle.FromXElement(XElement.Parse(serializedNamingStyle));

            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var span = context.Span;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var symbol = GetSymbol(root, span, syntaxFactsService, model, cancellationToken);
            if (symbol == null)
            {
                return;
            }

            var fixedNames = style.MakeCompliant(symbol.Name).ToImmutableArray();
            for (var i = 0; i < fixedNames.Length; i++)
            {
                var fixedName = fixedNames[i];
                // For the fix all provider, we only want to fix all the symbols for a certain rule.
                // Also encoding the index of the compliant names for this symbol.
                // It is used by the fix all provider to find the complaint name for all the symbols.
                var equivalenceKey = string.Concat(diagnostic.Properties["SymbolSpecificationID"], "|", i);
                context.RegisterCodeFix(
                    new FixNameCodeAction(
#if !CODE_STYLE
                        document.Project.Solution,
                        symbol,
                        fixedName,
#endif
                        string.Format(CodeFixesResources.Fix_name_violation_colon_0, fixedName),
                        c => FixAsync(document, symbol, fixedName, c),
                        equivalenceKey: equivalenceKey),
                    diagnostic);

            }
        }

        private static async Task<Solution> FixAsync(
            Document document, ISymbol symbol, string fixedName, CancellationToken cancellationToken)
        {
            return await Renamer.RenameSymbolAsync(
                document.Project.Solution, symbol, new SymbolRenameOptions(), fixedName,
                cancellationToken).ConfigureAwait(false);
        }

        private static ISymbol? GetSymbol(SyntaxNode root, TextSpan span, ISyntaxFactsService syntaxFactsService, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var node = root.FindNode(span);

            if (syntaxFactsService.IsIdentifierName(node))
            {
                // The location we get from the analyzer only contains the identifier token and when we get its containing node,
                // it is usually the right one (such as a variable declarator, designation or a foreach statement)
                // because there is no other node in between. But there is one case in a VB catch clause where the token
                // is wrapped in an identifier name. So if what we found is an identifier, take the parent node instead.
                // Note that this is the correct thing to do because GetDeclaredSymbol never works on identifier names.
                node = node.Parent;
            }

            if (node == null)
                return null;

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            // TODO: We should always be able to find the symbol that generated this diagnostic,
            // but this cannot always be done by simply asking for the declared symbol on the node 
            // from the symbol's declaration location.
            // See https://github.com/dotnet/roslyn/issues/16588
            return symbol;
        }

        private class FixNameCodeAction : CodeAction
        {
#if !CODE_STYLE
            private readonly Solution _startingSolution;
            private readonly ISymbol _symbol;
            private readonly string _newName;
#endif

            private readonly string _title;
            private readonly Func<CancellationToken, Task<Solution>> _createChangedSolutionAsync;
            private readonly string _equivalenceKey;

            public FixNameCodeAction(
#if !CODE_STYLE
                Solution startingSolution,
                ISymbol symbol,
                string newName,
#endif
                string title,
                Func<CancellationToken, Task<Solution>> createChangedSolutionAsync,
                string equivalenceKey)
            {
#if !CODE_STYLE
                _startingSolution = startingSolution;
                _symbol = symbol;
                _newName = newName;
#endif
                _title = title;
                _createChangedSolutionAsync = createChangedSolutionAsync;
                _equivalenceKey = equivalenceKey;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                return SpecializedCollections.SingletonEnumerable(
                    new ApplyChangesOperation(await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false)));
            }
            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var newSolution = await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
                var codeAction = new ApplyChangesOperation(newSolution);

#if CODE_STYLE  // https://github.com/dotnet/roslyn/issues/42218 tracks removing this conditional code.
                return SpecializedCollections.SingletonEnumerable(codeAction);
#else
                var factory = _startingSolution.Workspace.Services.GetRequiredService<ISymbolRenamedCodeActionOperationFactoryWorkspaceService>();
                return new CodeActionOperation[]
                {
                    codeAction,
                    factory.CreateSymbolRenamedOperation(_symbol, _newName, _startingSolution, newSolution)
                }.AsEnumerable();
#endif
            }

            public override string Title => _title;

            public override string EquivalenceKey => _equivalenceKey;
        }
    }
}
