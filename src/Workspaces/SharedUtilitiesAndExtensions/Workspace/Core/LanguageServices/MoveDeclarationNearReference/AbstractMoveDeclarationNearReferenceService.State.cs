// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveDeclarationNearReference
{
    internal partial class AbstractMoveDeclarationNearReferenceService<
        TService,
        TStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax>
    {
        private class State
        {
            public TLocalDeclarationStatementSyntax DeclarationStatement { get; private set; }
            public TVariableDeclaratorSyntax VariableDeclarator { get; private set; }
            public ILocalSymbol LocalSymbol { get; private set; }
            public SyntaxNode OutermostBlock { get; private set; }
            public SyntaxNode InnermostBlock { get; private set; }
            public IReadOnlyList<SyntaxNode> OutermostBlockStatements { get; private set; }
            public IReadOnlyList<SyntaxNode> InnermostBlockStatements { get; private set; }
            public TStatementSyntax FirstStatementAffectedInInnermostBlock { get; private set; }
            public int IndexOfDeclarationStatementInInnermostBlock { get; private set; }
            public int IndexOfFirstStatementAffectedInInnermostBlock { get; private set; }

            internal static async Task<State> GenerateAsync(
                TService service,
                Document document,
                TLocalDeclarationStatementSyntax statement,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeAsync(service, document, statement, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                TService service,
                Document document,
                TLocalDeclarationStatementSyntax node,
                CancellationToken cancellationToken)
            {
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var blockFacts = document.GetRequiredLanguageService<IBlockFactsService>();

                DeclarationStatement = node;

                var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(DeclarationStatement);
                if (variables.Count != 1)
                {
                    return false;
                }

                VariableDeclarator = (TVariableDeclaratorSyntax)variables[0];
                if (!service.IsValidVariableDeclarator(VariableDeclarator))
                {
                    return false;
                }

                OutermostBlock = blockFacts.GetStatementContainer(DeclarationStatement);
                if (!blockFacts.IsExecutableBlock(OutermostBlock))
                {
                    return false;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                LocalSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(
                    service.GetVariableDeclaratorSymbolNode(VariableDeclarator), cancellationToken);
                if (LocalSymbol == null)
                {
                    // This can happen in broken code, for example: "{ object x; object }"
                    return false;
                }

                var findReferencesResult = await SymbolFinder.FindReferencesAsync(LocalSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                var findReferencesList = findReferencesResult.ToList();
                if (findReferencesList.Count != 1)
                {
                    return false;
                }

                var references = findReferencesList[0].Locations.ToList();
                if (references.Count == 0)
                {
                    return false;
                }

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var referencingStatements =
                    (from r in references
                     let token = syntaxRoot.FindToken(r.Location.SourceSpan.Start)
                     let statement = token.GetAncestor<TStatementSyntax>()
                     where statement != null
                     select statement).ToSet();

                if (referencingStatements.Count == 0)
                {
                    return false;
                }

                InnermostBlock = blockFacts.FindInnermostCommonExecutableBlock(referencingStatements);
                if (InnermostBlock == null)
                {
                    return false;
                }

                InnermostBlockStatements = blockFacts.GetExecutableBlockStatements(InnermostBlock);
                OutermostBlockStatements = blockFacts.GetExecutableBlockStatements(OutermostBlock);

                var allAffectedStatements = new HashSet<TStatementSyntax>(referencingStatements.SelectMany(
                    expr => expr.GetAncestorsOrThis<TStatementSyntax>()));
                FirstStatementAffectedInInnermostBlock = InnermostBlockStatements.Cast<TStatementSyntax>().FirstOrDefault(allAffectedStatements.Contains);

                if (FirstStatementAffectedInInnermostBlock == null)
                {
                    return false;
                }

                if (FirstStatementAffectedInInnermostBlock == DeclarationStatement)
                {
                    return false;
                }

                IndexOfDeclarationStatementInInnermostBlock = InnermostBlockStatements.IndexOf(DeclarationStatement);
                IndexOfFirstStatementAffectedInInnermostBlock = InnermostBlockStatements.IndexOf(FirstStatementAffectedInInnermostBlock);
                if (IndexOfDeclarationStatementInInnermostBlock >= 0 &&
                    IndexOfDeclarationStatementInInnermostBlock < IndexOfFirstStatementAffectedInInnermostBlock)
                {
                    // Don't want to move a decl with initializer past other decls in order to move it to the first
                    // affected statement.  If we do we can end up in the following situation: 
#if false
                    int x = 0;
                    int y = 0;
                    Console.WriteLine(x + y);
#endif
                    // Each of these declarations will want to 'move' down to the WriteLine
                    // statement and we don't want to keep offering the refactoring.  Note: this
                    // solution is overly aggressive.  Technically if 'y' weren't referenced in
                    // Console.Writeline, then it might be a good idea to move the 'x'.  But this
                    // gives good enough behavior most of the time.

                    // Note that if the variable declaration has no initializer, then we still want to offer
                    // the move as the closest reference will be an assignment to the variable
                    // and we should be able to merge the declaration and assignment into a single
                    // statement.
                    // So, we also check if the variable declaration has an initializer below.

                    if (syntaxFacts.GetInitializerOfVariableDeclarator(VariableDeclarator) != null &&
                        InDeclarationStatementGroup(IndexOfDeclarationStatementInInnermostBlock, IndexOfFirstStatementAffectedInInnermostBlock))
                    {
                        return false;
                    }
                }

                var previousToken = FirstStatementAffectedInInnermostBlock.GetFirstToken().GetPreviousToken();
                var affectedSpan = TextSpan.FromBounds(previousToken.SpanStart, FirstStatementAffectedInInnermostBlock.Span.End);
                if (semanticModel.SyntaxTree.OverlapsHiddenPosition(affectedSpan, cancellationToken))
                {
                    return false;
                }

                return true;
            }

            private bool InDeclarationStatementGroup(
                int originalIndexInBlock, int firstStatementIndexAffectedInBlock)
            {
                for (var i = originalIndexInBlock; i < firstStatementIndexAffectedInBlock; i++)
                {
                    if (InnermostBlockStatements[i] is not TLocalDeclarationStatementSyntax)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
