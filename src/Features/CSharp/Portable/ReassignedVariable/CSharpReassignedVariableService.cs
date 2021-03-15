// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
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

        protected override DataFlowAnalysis? AnalyzeMethodBodyDataFlow(
            SemanticModel semanticModel, SyntaxNode methodDeclaration, CancellationToken cancellationToken)
        {
            var body = InitializeParameterHelpers.GetBody(methodDeclaration);
            if (body is BlockSyntax or ExpressionSyntax)
            {
                return semanticModel.AnalyzeDataFlow(body);
            }
            else if (body is ArrowExpressionClauseSyntax arrow)
            {
                return semanticModel.AnalyzeDataFlow(arrow.Expression);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(body);
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
