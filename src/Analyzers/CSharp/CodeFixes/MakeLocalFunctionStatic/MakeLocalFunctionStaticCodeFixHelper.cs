// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic;

using static SyntaxFactory;

internal static class MakeLocalFunctionStaticCodeFixHelper
{
    public static async Task<Document> MakeLocalFunctionStaticAsync(
        Document document,
        LocalFunctionStatementSyntax localFunction,
        ImmutableArray<ISymbol> captures,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxEditor = new SyntaxEditor(root, document.Project.Solution.Services);
        await MakeLocalFunctionStaticAsync(document, localFunction, captures, syntaxEditor, cancellationToken).ConfigureAwait(false);
        return document.WithSyntaxRoot(syntaxEditor.GetChangedRoot());
    }

    public static async Task MakeLocalFunctionStaticAsync(
        Document document,
        LocalFunctionStatementSyntax localFunction,
        ImmutableArray<ISymbol> captures,
        SyntaxEditor syntaxEditor,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var localFunctionSymbol = semanticModel.GetRequiredDeclaredSymbol(localFunction, cancellationToken);
        var documentImmutableSet = ImmutableHashSet.Create(document);

        // Finds all the call sites of the local function
        var referencedSymbols = await SymbolFinder.FindReferencesAsync(
            localFunctionSymbol, document.Project.Solution, documentImmutableSet, cancellationToken).ConfigureAwait(false);

        // Now we need to find all the references to the local function that we might need to fix.
        var shouldWarn = false;
        using var _ = ArrayBuilder<InvocationExpressionSyntax>.GetInstance(out var invocations);

        foreach (var referencedSymbol in referencedSymbols)
        {
            foreach (var location in referencedSymbol.Locations)
            {
                // We limited the search scope to the single document, 
                // so all reference should be in the same tree.
                var referenceNode = root.FindNode(location.Location.SourceSpan);
                if (referenceNode is not IdentifierNameSyntax identifierNode)
                {
                    // Unexpected scenario, skip and warn.
                    shouldWarn = true;
                    continue;
                }

                if (identifierNode.Parent is InvocationExpressionSyntax invocation)
                {
                    invocations.Add(invocation);
                }
                else
                {
                    // We won't be able to fix non-invocation references, 
                    // e.g. creating a delegate. 
                    shouldWarn = true;
                }
            }
        }

        var thisParameter = (IParameterSymbol?)captures.FirstOrDefault(c => c.IsThisParameter());

        var parameterAndCapturedSymbols = CreateParameterSymbols(captures.WhereAsArray(c => !c.IsThisParameter()));

        // Fix all invocations by passing in additional arguments.
        foreach (var invocation in invocations)
        {
            syntaxEditor.ReplaceNode(
                invocation,
                (node, generator) =>
                {
                    var currentInvocation = (InvocationExpressionSyntax)node;
                    var seenNamedArgument = currentInvocation.ArgumentList.Arguments.Any(a => a.NameColon != null);
                    var seenDefaultArgumentValue = currentInvocation.ArgumentList.Arguments.Count < localFunction.ParameterList.Parameters.Count;

                    // Add all the non-this parameters to the end.  If there is a 'this' parameter, add it to the start.
                    var newArguments = parameterAndCapturedSymbols.Where(p => !p.symbol.IsThisParameter()).Select(
                        symbolAndCapture => (ArgumentSyntax)generator.Argument(
                            seenNamedArgument || seenDefaultArgumentValue ? symbolAndCapture.symbol.Name : null,
                            symbolAndCapture.symbol.RefKind,
                            symbolAndCapture.capture.Name.ToIdentifierName()));

                    var newArgumentsList = currentInvocation.ArgumentList.Arguments.AddRange(newArguments);
                    if (thisParameter != null)
                        newArgumentsList = newArgumentsList.Insert(0, (ArgumentSyntax)generator.Argument(generator.ThisExpression()));

                    var newArgList = currentInvocation.ArgumentList.WithArguments(newArgumentsList);
                    return currentInvocation.WithArgumentList(newArgList);
                });
        }

        // In case any of the captured variable isn't camel-cased,
        // we need to change the referenced name inside local function to use the new parameter's name.
        foreach (var (parameter, capture) in parameterAndCapturedSymbols)
        {
            if (parameter.Name == capture.Name)
                continue;

            var referencedCaptureSymbols = await SymbolFinder.FindReferencesAsync(
                capture, document.Project.Solution, documentImmutableSet, cancellationToken).ConfigureAwait(false);

            foreach (var referencedSymbol in referencedCaptureSymbols)
            {
                foreach (var location in referencedSymbol.Locations)
                {
                    var referenceSpan = location.Location.SourceSpan;
                    if (!localFunction.FullSpan.Contains(referenceSpan))
                        continue;

                    var referenceNode = root.FindNode(referenceSpan);
                    if (referenceNode is IdentifierNameSyntax identifierNode)
                    {
                        syntaxEditor.ReplaceNode(
                            identifierNode,
                            (node, generator) => IdentifierName(parameter.Name.ToIdentifierToken()).WithTriviaFrom(node));
                    }
                }
            }
        }

        // If we captured 'this', then we have to go through and rewrite all usages of it to @this.  Note that
        // 'this' may be used explicitly or implicitly.
        if (thisParameter != null)
        {
            var localFunctionBodyOperation = semanticModel.GetOperation(localFunction.Body ?? (SyntaxNode)localFunction.ExpressionBody!.Expression, cancellationToken);
            foreach (var descendent in localFunctionBodyOperation.DescendantsAndSelf())
            {
                if (descendent is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } instanceReference)
                {
                    if (!instanceReference.IsImplicit)
                    {
                        syntaxEditor.ReplaceNode(instanceReference.Syntax, IdentifierName("@this"));
                    }
                    else if (instanceReference.Syntax is SimpleNameSyntax name)
                    {
                        syntaxEditor.ReplaceNode(name, MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("@this"), name));
                    }
                }
            }
        }

#if CODE_STYLE
        var info = new CSharpCodeGenerationContextInfo(
            CodeGenerationContext.Default, CSharpCodeGenerationOptions.Default, new CSharpCodeGenerationService(document.Project.GetExtendedLanguageServices().LanguageServices), root.SyntaxTree.Options.LanguageVersion());
#else
        var info = await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(false);
#endif

        // Updates the local function declaration with variables passed in as parameters
        syntaxEditor.ReplaceNode(
            localFunction,
            (node, generator) =>
            {
                var localFunctionWithNewParameters = (LocalFunctionStatementSyntax)info.Service.AddParameters(
                    node,
                    parameterAndCapturedSymbols.SelectAsArray(p => p.symbol),
                    info,
                    cancellationToken);

                // Add @this parameter as the first parameter to the local function.
                if (thisParameter != null)
                {
                    var parameterList = localFunctionWithNewParameters.ParameterList;
                    var parameters = parameterList.Parameters;
                    localFunctionWithNewParameters = localFunctionWithNewParameters.ReplaceNode(
                        parameterList, parameterList.WithParameters(parameters.Insert(0, Parameter(Identifier("@this")).WithType(thisParameter.Type.GenerateTypeSyntax()))));
                }

                if (shouldWarn)
                {
                    var annotation = WarningAnnotation.Create(CSharpCodeFixesResources.Warning_colon_Adding_parameters_to_local_function_declaration_may_produce_invalid_code);
                    localFunctionWithNewParameters = localFunctionWithNewParameters.WithAdditionalAnnotations(annotation);
                }

                return AddStaticModifier(localFunctionWithNewParameters, generator);
            });
    }

    public static SyntaxNode AddStaticModifier(SyntaxNode localFunction, SyntaxGenerator generator)
        => generator.WithModifiers(
            localFunction,
            generator.GetModifiers(localFunction).WithIsStatic(true));

    /// <summary>
    /// Creates a new parameter symbol paired with the original captured symbol for each captured variables.
    /// </summary>
    private static ImmutableArray<(IParameterSymbol symbol, ISymbol capture)> CreateParameterSymbols(ImmutableArray<ISymbol> captures)
        => captures.SelectAsArray(static capture =>
        {
            var symbolType = capture.GetSymbolType();
            Contract.ThrowIfNull(symbolType);

            return (CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: RefKind.None,
                isParams: false,
                type: symbolType,
                name: capture.Name.ToCamelCase()), capture);
        });
}
