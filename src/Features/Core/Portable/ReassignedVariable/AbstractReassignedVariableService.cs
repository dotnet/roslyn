// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ReassignedVariable
{
    internal abstract class AbstractReassignedVariableService<
        TParameterSyntax,
        TVariableDeclaratorSyntax,
        TVariableSyntax,
        TIdentifierNameSyntax>
        : IReassignedVariableService
        where TParameterSyntax : SyntaxNode
        where TVariableDeclaratorSyntax : SyntaxNode
        where TVariableSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        protected abstract void AddVariables(TVariableDeclaratorSyntax declarator, ref TemporaryArray<TVariableSyntax> temporaryArray);
        protected abstract SyntaxNode GetParentScope(SyntaxNode localDeclaration);
        protected abstract SyntaxToken GetIdentifierOfVariable(TVariableSyntax variable);
        protected abstract DataFlowAnalysis? AnalyzeMethodBodyDataFlow(SemanticModel semanticModel, SyntaxNode methodBlock, CancellationToken cancellationToken);

        public async Task<ImmutableArray<TextSpan>> GetReassignedVariablesAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = PooledDictionary<ISymbol, bool>.GetInstance(out var symbolToIsReassigned);
            using var _2 = ArrayBuilder<TextSpan>.GetInstance(out var result);
            using var _3 = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);

            // First, walk through all identifiers in the provided span.  If they refer to a local/param, then determine
            // if that's a local/param that's reassigned or not.  Note that the local/param they refer to may not be in the
            // span being asked about.
            stack.Add(root.FindNode(span));

            while (stack.Count > 0)
            {
                var current = stack.Last();
                stack.RemoveLast();

                if (current.Span.IntersectsWith(span))
                {
                    ProcessNode(current);

                    foreach (var child in current.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                            stack.Add(child.AsNode()!);
                    }
                }
            }

            return result.ToImmutable();

            void ProcessNode(SyntaxNode node)
            {
                switch (node)
                {
                    case TIdentifierNameSyntax identifier:
                        ProcessIdentifier(identifier);
                        break;
                    case TParameterSyntax parameter:
                        ProcessParameter(parameter);
                        break;
                    case TVariableDeclaratorSyntax variable:
                        ProcessVariable(variable);
                        break;
                }
            }

            void ProcessIdentifier(TIdentifierNameSyntax identifier)
            {
                // Don't bother even looking at identifiers that aren't standalone (i.e. they're not on the left of some
                // expression).  These could not refer to locals or fields.
                if (syntaxFacts.GetStandaloneExpression(identifier) != identifier)
                    return;

                var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (symbol == null)
                    return;

                if (IsSymbolReassigned(symbol))
                    result.Add(identifier.Span);
            }

            void ProcessParameter(TParameterSyntax parameterSyntax)
            {
                var parameter = semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken);
                if (IsSymbolReassigned(parameter))
                    result.Add(syntaxFacts.GetIdentifierOfParameter(parameterSyntax).Span);
            }

            void ProcessVariable(TVariableDeclaratorSyntax declarator)
            {
                using var variables = TemporaryArray<TVariableSyntax>.Empty;
                AddVariables(declarator, ref variables.AsRef());

                foreach (var variable in variables)
                {
                    var local = semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                    if (IsSymbolReassigned(local))
                        result.Add(GetIdentifierOfVariable(variable).Span);
                }
            }

            bool IsSymbolReassigned([NotNullWhen(true)] ISymbol? symbol)
            {
                // Note: we don't need to test range variables, and they are never reassignable.
                if (symbol is not IParameterSymbol and not ILocalSymbol)
                    return false;

                if (!symbolToIsReassigned.TryGetValue(symbol, out var reassignedResult))
                {
                    reassignedResult = ComputeIsAssigned(symbol);
                    symbolToIsReassigned[symbol] = reassignedResult;
                }

                return reassignedResult;
            }

            bool ComputeIsAssigned(ISymbol symbol)
            {
                return symbol switch
                {
                    IParameterSymbol parameter => ComputeParameterIsAssigned(parameter),
                    ILocalSymbol local => ComputeLocalIsAssigned(local),
                    _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
                };
            }

            bool ComputeParameterIsAssigned(IParameterSymbol parameter)
            {
                // Parameters are an easy case.  We just need to get the method they're defined for, and see if they are
                // even written to inside that method.

                if (parameter.ContainingSymbol is not IMethodSymbol method)
                    return false;

                if (method.DeclaringSyntaxReferences.Length == 0)
                    return false;

                var methodDeclaration = method.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);

                // Potentially a reference to a parameter in another file.
                if (methodDeclaration.SyntaxTree != semanticModel.SyntaxTree)
                    return false;

                var dataFlow = AnalyzeMethodBodyDataFlow(semanticModel, methodDeclaration, cancellationToken);
                if (dataFlow == null)
                    return false;

                foreach (var methodParam in method.Parameters)
                    symbolToIsReassigned[methodParam] = dataFlow.WrittenInside.Contains(methodParam);

                return symbolToIsReassigned.TryGetValue(parameter, out var result) && result;
            }

            bool ComputeLocalIsAssigned(ILocalSymbol local)
            {
                // Locals are harder to determine than parameters.  Because a parameter always comes in assigned, we only
                // have to see if there is a later assignment.  For locals though, they may start unassigned, and then only
                // get assigned once later.  That would not count as a reassignment.  So we only want to consider something
                // a reassignment, if it is *already* assigned, and then written again.

                if (local.DeclaringSyntaxReferences.Length == 0)
                    return false;

                var localDeclaration = local.DeclaringSyntaxReferences.First().GetSyntax(cancellationToken);

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
}
