// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveDeclarationNearReference
{
    internal partial class MoveDeclarationNearReferenceCodeRefactoringProvider
    {
        private class State
        {
            public LocalDeclarationStatementSyntax DeclarationStatement { get; private set; }
            public VariableDeclarationSyntax VariableDeclaration { get; private set; }
            public VariableDeclaratorSyntax VariableDeclarator { get; private set; }
            public ILocalSymbol LocalSymbol { get; private set; }
            public BlockSyntax OutermostBlock { get; private set; }
            public BlockSyntax InnermostBlock { get; private set; }
            public StatementSyntax FirstStatementAffectedInInnermostBlock { get; private set; }

            internal static async Task<State> GenerateAsync(
                SemanticDocument document,
                LocalDeclarationStatementSyntax statement,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeAsync(document, statement, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                SemanticDocument document,
                LocalDeclarationStatementSyntax node,
                CancellationToken cancellationToken)
            {
                if (node == null)
                {
                    return false;
                }

                this.DeclarationStatement = node;
                if (!(this.DeclarationStatement.IsParentKind(SyntaxKind.Block) &&
                      this.DeclarationStatement.Declaration.Variables.Count == 1))
                {
                    return false;
                }

                this.VariableDeclaration = this.DeclarationStatement.Declaration;
                this.VariableDeclarator = this.VariableDeclaration.Variables[0];
                this.OutermostBlock = (BlockSyntax)this.DeclarationStatement.Parent;
                this.LocalSymbol = (ILocalSymbol)document.SemanticModel.GetDeclaredSymbol(this.VariableDeclarator, cancellationToken);
                if (this.LocalSymbol == null)
                {
                    // This can happen in broken code, for example: "{ object x; object }"
                    return false;
                }

                var findReferencesResult = await SymbolFinder.FindReferencesAsync(this.LocalSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
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

                var syntaxTree = document.SyntaxTree;
                var referencingStatements =
                    (from r in references
                     let token = document.Root.FindToken(r.Location.SourceSpan.Start)
                     let statement = token.GetAncestor<StatementSyntax>()
                     where statement != null
                     select statement).ToList();

                if (referencingStatements.Count == 0)
                {
                    return false;
                }

                this.InnermostBlock = referencingStatements.FindInnermostCommonBlock();

                var allAffectedStatements = new HashSet<StatementSyntax>(referencingStatements.SelectMany(expr => expr.GetAncestorsOrThis<StatementSyntax>()));
                this.FirstStatementAffectedInInnermostBlock = this.InnermostBlock.Statements.FirstOrDefault(allAffectedStatements.Contains);

                if (this.FirstStatementAffectedInInnermostBlock == null)
                {
                    return false;
                }

                if (this.FirstStatementAffectedInInnermostBlock == this.DeclarationStatement)
                {
                    return false;
                }

                var originalIndexInBlock = this.InnermostBlock.Statements.IndexOf(this.DeclarationStatement);
                var firstStatementIndexAffectedInBlock = this.InnermostBlock.Statements.IndexOf(this.FirstStatementAffectedInInnermostBlock);
                if (originalIndexInBlock >= 0 &&
                    originalIndexInBlock < firstStatementIndexAffectedInBlock)
                {
                    // Don't want to move a decl past other decls in order to move it to the first
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
                    if (InDeclarationStatementGroup(originalIndexInBlock, firstStatementIndexAffectedInBlock))
                    {
                        return false;
                    }
                }

                var previousNodeOrToken = firstStatementIndexAffectedInBlock == 0
                    ? this.InnermostBlock.OpenBraceToken
                    : (SyntaxNodeOrToken)this.InnermostBlock.Statements[firstStatementIndexAffectedInBlock - 1];
                var affectedSpan = TextSpan.FromBounds(previousNodeOrToken.SpanStart, FirstStatementAffectedInInnermostBlock.Span.End);
                if (syntaxTree.OverlapsHiddenPosition(affectedSpan, cancellationToken))
                {
                    return false;
                }

                return true;
            }

            private bool InDeclarationStatementGroup(int originalIndexInBlock, int firstStatementIndexAffectedInBlock)
            {
                for (var i = originalIndexInBlock; i < firstStatementIndexAffectedInBlock; i++)
                {
                    if (!(this.InnermostBlock.Statements[i] is LocalDeclarationStatementSyntax))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
