// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.NamingStyles
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.ApplyNamingStyle), Shared]
#pragma warning disable RS1016 // Code fix providers should provide FixAll support. https://github.com/dotnet/roslyn/issues/23528
    internal class NamingStyleCodeFixProvider : CodeFixProvider
#pragma warning restore RS1016 // Code fix providers should provide FixAll support.
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.NamingRuleId);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var serializedNamingStyle = diagnostic.Properties[nameof(NamingStyle)];
            var style = NamingStyle.FromXElement(XElement.Parse(serializedNamingStyle));

            var document = context.Document;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span);

            if (document.GetLanguageService<ISyntaxFactsService>().IsIdentifierName(node))
            {
                // The location we get from the analyzer only contains the identifier token and when we get its containing node,
                // it is usually the right one (such as a variable declarator, designation or a foreach statement)
                // because there is no other node in between. But there is one case in a VB catch clause where the token
                // is wrapped in an identifier name. So if what we found is an identifier, take the parent node instead.
                // Note that this is the correct thing to do because GetDeclaredSymbol never works on identifier names.
                node = node.Parent;
            }

            var model = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var symbol = model.GetDeclaredSymbol(node, context.CancellationToken);

            // TODO: We should always be able to find the symbol that generated this diagnostic,
            // but this cannot always be done by simply asking for the declared symbol on the node 
            // from the symbol's declaration location.
            // See https://github.com/dotnet/roslyn/issues/16588

            if (symbol == null)
            {
                return;
            }

            var fixedNames = style.MakeCompliant(symbol.Name);
            foreach (var fixedName in fixedNames)
            {
                var solution = context.Document.Project.Solution;
                context.RegisterCodeFix(
                    new FixNameCodeAction(
                        solution,
                        symbol,
                        fixedName,
                        string.Format(FeaturesResources.Fix_Name_Violation_colon_0, fixedName),
                        c => FixAsync(document, symbol, fixedName, c),
                        equivalenceKey: nameof(NamingStyleCodeFixProvider)),
                    diagnostic);
            }
        }

        private static async Task<Solution> FixAsync(
            Document document, ISymbol symbol, string fixedName, CancellationToken cancellationToken)
        {
            return await Renamer.RenameSymbolAsync(
                document.Project.Solution, symbol, fixedName,
                await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
        }

        private class FixNameCodeAction : CodeAction
        {
            private readonly Solution _startingSolution;
            private readonly ISymbol _symbol;
            private readonly string _newName;
            private readonly string _title;
            private readonly Func<CancellationToken, Task<Solution>> _createChangedSolutionAsync;
            private readonly string _equivalenceKey;

            public FixNameCodeAction(
                Solution startingSolution, 
                ISymbol symbol, 
                string newName, 
                string title, 
                Func<CancellationToken, Task<Solution>> createChangedSolutionAsync, 
                string equivalenceKey)
            {
                _startingSolution = startingSolution;
                _symbol = symbol;
                _newName = newName;
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
                var factory =_startingSolution.Workspace.Services.GetService<ISymbolRenamedCodeActionOperationFactoryWorkspaceService>();
                var newSolution = await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
                return new CodeActionOperation[]
                {
                    new ApplyChangesOperation(newSolution),
                    factory.CreateSymbolRenamedOperation(_symbol, _newName, _startingSolution, newSolution)
                }.AsEnumerable();
            }

            public override string Title => _title;

            public override string EquivalenceKey => _equivalenceKey;
        }
    }
}
