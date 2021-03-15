// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReassignedVariable
{
    internal abstract class AbstractReassignedVariableService<
        TParameterDeclaration,
        TVariableDeclarator,
        TIdentifierNameSyntax>
        : IReassignedVariableService
        where TParameterDeclaration : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        protected abstract SyntaxNode GetParentScope(SyntaxNode localDeclaration);
        protected abstract SyntaxNode GetMethodBlock(SyntaxNode methodDeclaration);
        protected abstract DataFlowAnalysis AnalyzeMethodBodyDataFlow(SyntaxNode methodBlock, CancellationToken cancellationToken);

        public async Task<ImmutableArray<TextSpan>> GetReassignedVariablesAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = PooledDictionary<ISymbol, bool>.GetInstance(out var symbolToIsReassigned);
            using var _2 = ArrayBuilder<TextSpan>.GetInstance(out var result);

            var identifiers = root.DescendantNodesAndSelf(span).OfType<TIdentifierNameSyntax>();
            foreach (var identifier in identifiers)
            {
                if (await IsReassignedAsync(semanticFacts, syntaxFacts, semanticModel, identifier, symbolToIsReassigned, cancellationToken).ConfigureAwait(false))
                    result.Add(identifier.Span);
            }

            var parameterDecls = root.DescendantNodes(span).OfType<TParameterDeclaration>();
            foreach (var decl in parameterDecls)
            {
                var parameter = semanticModel.GetDeclaredSymbol(decl, cancellationToken) as IParameterSymbol;
                if (parameter == null || parameter.Locations.Length == 0)
                    continue;

                Contract.ThrowIfNull(parameter);

                var isReassigned = await IsReassignedAsync(
                    semanticFacts, syntaxFacts, semanticModel, symbolToIsReassigned, parameter, cancellationToken).ConfigureAwait(false);
                if (isReassigned)
                    result.Add(syntaxFacts.GetIdentifierOfParameterDeclaration(decl).Span);
            }

            var variableDecls = root.DescendantNodes(span).OfType<TVariableDeclarator>();
            foreach (var decl in variableDecls)
            {
                var local = semanticModel.GetDeclaredSymbol(decl, cancellationToken);
                if (local == null || local.Locations.Length == 0)
                    continue;

                var isReassigned = await IsReassignedAsync(
                    semanticFacts, syntaxFacts, semanticModel, symbolToIsReassigned, local, cancellationToken).ConfigureAwait(false);
                if (isReassigned)
                    result.Add(syntaxFacts.GetIdentifierOfVariableDeclarator(decl).Span);
            }

            return result.ToImmutable();
        }

        private Task<bool> IsReassignedAsync(
            ISemanticFactsService semanticFacts,
            ISyntaxFactsService syntaxFacts,
            SemanticModel semanticModel,
            TIdentifierNameSyntax identifier,
            Dictionary<ISymbol, bool> symbolToIsReassigned,
            CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (symbol == null)
                return SpecializedTasks.False;

            return IsReassignedAsync(
                semanticFacts, syntaxFacts, semanticModel, symbolToIsReassigned, symbol, cancellationToken);
        }

        private async Task<bool> IsReassignedAsync(
            ISemanticFactsService semanticFacts,
            ISyntaxFactsService syntaxFacts,
            SemanticModel semanticModel,
            Dictionary<ISymbol, bool> symbolToIsReassigned,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            // Note: we don't need to test range variables, and they are never reassignable.
            if (symbol is not IParameterSymbol and not ILocalSymbol)
                return false;

            if (!symbolToIsReassigned.TryGetValue(symbol, out var reassignedResult))
            {
                reassignedResult = await ComputeIsAssignedAsync(
                    semanticFacts, syntaxFacts, semanticModel, symbol, symbolToIsReassigned, cancellationToken).ConfigureAwait(false);
                symbolToIsReassigned[symbol] = reassignedResult;
            }

            return reassignedResult;
        }

        private Task<bool> ComputeIsAssignedAsync(
            ISemanticFactsService semanticFacts,
            ISyntaxFactsService syntaxFacts,
            SemanticModel semanticModel,
            ISymbol symbol,
            Dictionary<ISymbol, bool> symbolToIsReassigned,
            CancellationToken cancellationToken)
        {
            return symbol switch
            {
                IParameterSymbol parameter => ComputeParameterIsAssignedAsync(semanticModel, parameter, symbolToIsReassigned, cancellationToken),
                ILocalSymbol local => ComputeLocalIsAssignedAsync(semanticFacts, syntaxFacts, semanticModel, local, cancellationToken),
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
            };
        }

        private async Task<bool> ComputeParameterIsAssignedAsync(
            SemanticModel semanticModel,
            IParameterSymbol parameter,
            Dictionary<ISymbol, bool> symbolToIsReassigned,
            CancellationToken cancellationToken)
        {
            // Parameters are an easy case.  We just need to get the method they're defined for, and see if they are
            // even written to inside that method.

            if (parameter.ContainingSymbol is not IMethodSymbol method)
                return false;

            if (method.DeclaringSyntaxReferences.Length == 0)
                return false;

            var methodDeclaration = await method.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            // Potentially a reference to a parameter in another file.
            if (methodDeclaration.SyntaxTree != semanticModel.SyntaxTree)
                return false;

            var methodBlock = GetMethodBlock(methodDeclaration);
            var dataFlow = AnalyzeMethodBodyDataFlow(methodBlock, cancellationToken);

            foreach (var methodParam in method.Parameters)
                symbolToIsReassigned[methodParam] = dataFlow.WrittenInside.Contains(methodParam);

            return symbolToIsReassigned.TryGetValue(parameter, out var result) && result;
        }

        private async Task<bool> ComputeLocalIsAssignedAsync(
            ISemanticFactsService semanticFacts,
            ISyntaxFactsService syntaxFacts,
            SemanticModel semanticModel,
            ILocalSymbol local,
            CancellationToken cancellationToken)
        {
            // Locals are harder to determine than parameters.  Because a parameter always comes in assigned, we only
            // have to see if there is a later assignment.  For locals though, they may start unassigned, and then only
            // get assigned once later.  That would not count as a reassignment.  So we only want to consider something
            // a reassignment, if it is *already* assigned, and then written again.

            if (local.DeclaringSyntaxReferences.Length == 0)
                return false;

            var localDeclaration = await local.DeclaringSyntaxReferences.First().GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            if (localDeclaration.SyntaxTree != semanticModel.SyntaxTree)
            {
                Contract.Fail("Local did not come from same file that we were analyzing?");
                return false;
            }

            // Get the scope the local is declared in.
            var parentScope = GetParentScope(localDeclaration);

            // Now, walk the scope, looking for all usages of the local.  See if any are a reassignment.
            foreach (var id in parentScope.DescendantNodes().OfType<TIdentifierNameSyntax>())
            {
                // Ignore any nodes before the local decl.
                if (id.SpanStart <= localDeclaration.SpanStart)
                    continue;

                // Ignore identifiers that don't match the local name.
                var idToken = syntaxFacts.GetIdentifierOfSimpleName(id);
                if (!syntaxFacts.StringComparer.Equals(idToken.ValueText, local.Name))
                    continue;

                // Ignore identifiers that bind to another symbol.
                var symbol = semanticModel.GetSymbolInfo(id, cancellationToken).Symbol;
                if (!local.Equals(symbol))
                    continue;

                // Ok, we have a reference to the local.  See if it was assigned on entry.  If not, we don't care about
                // this reference. As an assignment here doesn't mean it was reassigned.
                var dataFlow = semanticModel.AnalyzeDataFlow(id);
                if (!dataFlow.DefinitelyAssignedOnEntry.Contains(local))
                    continue;

                // This was a variable that was already assigned prior to this location.  See if this location is
                // considered a write.
                if (semanticFacts.IsWrittenTo(semanticModel, id, cancellationToken))
                    return true;
            }

            return false;
        }
    }
}
