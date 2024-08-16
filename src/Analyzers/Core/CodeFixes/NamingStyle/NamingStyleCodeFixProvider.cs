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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Collections;

#if !CODE_STYLE  // https://github.com/dotnet/roslyn/issues/42218 removing dependency on WorkspaceServices.
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
#endif

namespace Microsoft.CodeAnalysis.CodeFixes.NamingStyles;

#if !CODE_STYLE  // https://github.com/dotnet/roslyn/issues/42218 tracks enabling this fixer in CodeStyle layer.
[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
    Name = PredefinedCodeFixProviderNames.ApplyNamingStyle), Shared]
#endif
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class NamingStyleCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.NamingRuleId];

    public override FixAllProvider? GetFixAllProvider()
    {
        // Currently Fix All is not supported for naming style violations.
        return null;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var serializedNamingStyle = diagnostic.Properties[nameof(NamingStyle)];
        Contract.ThrowIfNull(serializedNamingStyle);

        var style = NamingStyle.FromXElement(XElement.Parse(serializedNamingStyle));

        var document = context.Document;
        var span = context.Span;

        var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root.FindNode(span);

        if (document.GetRequiredLanguageService<ISyntaxFactsService>().IsIdentifierName(node))
        {
            // The location we get from the analyzer only contains the identifier token and when we get its containing node,
            // it is usually the right one (such as a variable declarator, designation or a foreach statement)
            // because there is no other node in between. But there is one case in a VB catch clause where the token
            // is wrapped in an identifier name. So if what we found is an identifier, take the parent node instead.
            // Note that this is the correct thing to do because GetDeclaredSymbol never works on identifier names.
            node = node.Parent;
        }

        if (node == null)
            return;

        var model = await document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
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
            context.RegisterCodeFix(
                new FixNameCodeAction(
#if !CODE_STYLE
                    document.Project.Solution,
                    symbol,
                    fixedName,
#endif
                    string.Format(CodeFixesResources.Fix_name_violation_colon_0, fixedName),
                    c => FixAsync(document, symbol, fixedName, c),
                    equivalenceKey: nameof(NamingStyleCodeFixProvider)),
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

        /// <summary>
        /// This code action does produce non-text-edit operations (like notifying 3rd parties about a rename).  But
        /// it doesn't require this.  As such, we can allow it to run in hosts that only allow document edits. Those
        /// hosts will simply ignore the operations they don't understand.
        /// </summary>
        public override ImmutableArray<string> Tags => [];

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
            => [new ApplyChangesOperation(await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false))];

        protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var newSolution = await _createChangedSolutionAsync(cancellationToken).ConfigureAwait(false);
            var codeAction = new ApplyChangesOperation(newSolution);

#if CODE_STYLE  // https://github.com/dotnet/roslyn/issues/42218 tracks removing this conditional code.
            return [codeAction];
#else

            using var operations = TemporaryArray<CodeActionOperation>.Empty;

            operations.Add(codeAction);
            var factory = _startingSolution.Services.GetService<ISymbolRenamedCodeActionOperationFactoryWorkspaceService>();
            if (factory is not null)
            {
                operations.Add(factory.CreateSymbolRenamedOperation(_symbol, _newName, _startingSolution, newSolution));
            }

            return operations.ToImmutableAndClear();
#endif
        }

        public override string Title => _title;

        public override string EquivalenceKey => _equivalenceKey;
    }
}
