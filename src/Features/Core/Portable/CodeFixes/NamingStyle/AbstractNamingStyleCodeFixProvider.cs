// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Rename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.NamingStyles
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.ApplyNamingStyle), Shared]
    internal class NamingStyleCodeFixProvider : CodeFixProvider
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
                        string.Format(FeaturesResources.Fix_Name_Violation_colon_0, fixedName),
                        c => FixAsync(document, symbol, fixedName, c),
                        nameof(NamingStyleCodeFixProvider)),
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

        private class FixNameCodeAction : CodeAction.SolutionChangeAction
        {
            public FixNameCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}