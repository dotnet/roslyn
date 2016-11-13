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
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
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

            var fixedNames = style.MakeCompliant(symbol.Name);
            foreach (var fixedName in fixedNames)
            {
                var solution = context.Document.Project.Solution;
                context.RegisterCodeFix(
                    new FixNameCodeAction(
                        string.Format(FeaturesResources.Fix_Name_Violation_colon_0, fixedName),
                        async c => await Renamer.RenameSymbolAsync(
                            solution,
                            symbol,
                            fixedName,
                            await document.GetOptionsAsync(c).ConfigureAwait(false),
                            c).ConfigureAwait(false),
                        nameof(NamingStyleCodeFixProvider)),
                    diagnostic);
            }
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