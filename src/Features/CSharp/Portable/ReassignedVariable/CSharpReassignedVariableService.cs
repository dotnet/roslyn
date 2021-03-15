// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReassignedVariable;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReassignedVariable
{
    [ExportLanguageService(typeof(IReassignedVariableService), LanguageNames.CSharp), Shared]
    internal class CSharpReassignedVariableService : AbstractReassignedVariableService<
        ParameterSyntax,
        VariableDeclaratorSyntax,
        VariableDeclaratorSyntax,
        IdentifierNameSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpReassignedVariableService()
        {
        }

        protected override void AddVariables(VariableDeclaratorSyntax declarator, ref TemporaryArray<VariableDeclaratorSyntax> variables)
            => variables.Add(declarator);

        protected override SyntaxToken GetIdentifierOfVariable(VariableDeclaratorSyntax variable)
            => variable.Identifier;

        protected override void AnalyzeMemberBodyDataFlow(
            SemanticModel semanticModel,
            SyntaxNode member,
            ref TemporaryArray<DataFlowAnalysis?> dataFlowAnalyses,
            CancellationToken cancellationToken)
        {
            using var bodies = TemporaryArray<SyntaxNode>.Empty;
            AddBodies(member, ref bodies.AsRef());

            foreach (var body in bodies)
            {
                if (body is null)
                    continue;

                if (body is BlockSyntax or ExpressionSyntax)
                {
                    dataFlowAnalyses.Add(semanticModel.AnalyzeDataFlow(body));
                }
                else if (body is ArrowExpressionClauseSyntax arrow)
                {
                    dataFlowAnalyses.Add(semanticModel.AnalyzeDataFlow(arrow.Expression));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(body);
                }
            }
        }

        private static void AddBodies(SyntaxNode? declaration, ref TemporaryArray<SyntaxNode> bodies)
        {
            switch (declaration)
            {
                case null:
                    return;

                case AccessorDeclarationSyntax accessor:
                    bodies.AddIfNotNull(accessor.Body);
                    AddBodies(accessor.ExpressionBody, ref bodies);
                    break;

                case ArrowExpressionClauseSyntax arrowExpressionClause:
                    bodies.Add(arrowExpressionClause.Expression);
                    break;

                case BaseMethodDeclarationSyntax methodDeclaration:
                    bodies.AddIfNotNull(methodDeclaration.Body);
                    AddBodies(methodDeclaration.ExpressionBody, ref bodies);
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    bodies.AddIfNotNull(localFunction.Body);
                    AddBodies(localFunction.ExpressionBody, ref bodies);
                    break;

                case AnonymousFunctionExpressionSyntax anonymousFunction:
                    bodies.AddIfNotNull(anonymousFunction.Block);
                    bodies.AddIfNotNull(anonymousFunction.ExpressionBody);
                    break;

                case IndexerDeclarationSyntax indexer:
                    bodies.AddIfNotNull(indexer.ExpressionBody?.Expression);
                    if (indexer.AccessorList != null)
                    {
                        foreach (var accessor in indexer.AccessorList.Accessors)
                            AddBodies(accessor, ref bodies);
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(declaration);
            }
        }

        protected override SyntaxNode GetParentScope(SyntaxNode localDeclaration)
        {
            var current = localDeclaration;
            while (current != null)
            {
                if (current is StatementSyntax or SwitchSectionSyntax or ArrowExpressionClauseSyntax or MemberDeclarationSyntax)
                    break;

                current = current.Parent;
            }

            Contract.ThrowIfNull(current, "Couldn't find a suitable parent of this local declaration");
            if (current is LocalDeclarationStatementSyntax)
                return current.GetRequiredParent();

            return current;
        }
    }
}
