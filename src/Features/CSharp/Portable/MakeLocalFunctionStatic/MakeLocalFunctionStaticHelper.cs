// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Wrapping;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    internal sealed class MakeLocalFunctionStaticHelper
    {
        internal static async Task<Document> CreateParameterSymbolAsync(Document document, LocalFunctionStatementSyntax localFunction, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(true);
            var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
            var dataFlow = semanticModel.AnalyzeDataFlow(localFunction);
            var captures = dataFlow.CapturedInside;

            var parameters = CreateParameterSymbol(captures);

            // Finds all the call sites of the local function
            var arrayNode = await SymbolFinder.FindReferencesAsync
                (localFunctionSymbol, document.Project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            var rootOne = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(rootOne, CSharpSyntaxGenerator.Instance);

            editor.TrackNode(localFunction);

            var referencesBuilder = Analyzer.Utilities.PooledObjects.ArrayBuilder<InvocationExpressionSyntax>.GetInstance();
            foreach (var referenceSymbol in arrayNode)
            {
                foreach (var location in referenceSymbol.Locations)
                {
                    var root = await location.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = root.FindNode(location.Location.SourceSpan); //Node for the identifier syntax

                    var invocation = (syntaxNode as IdentifierNameSyntax).Parent as InvocationExpressionSyntax;
                    if (invocation == null)
                    {
                        var annotation = WarningAnnotation.Create("Warning: Expression may have side effects. Code meaning may change.");
                        editor.ReplaceNode(syntaxNode, syntaxNode.WithAdditionalAnnotations(annotation));
                        continue;
                    }

                    editor.TrackNode(invocation);
                    referencesBuilder.Add(invocation);
                }
            }

            foreach (var invocation in referencesBuilder.OrderByDescending(n => n.Span.Start))
            {
                var newArguments = parameters.Select
                    (p => CSharpSyntaxGenerator.Instance.Argument(name: null, p.RefKind, p.Name.ToIdentifierName()) as ArgumentSyntax);
                var newArgList = invocation.ArgumentList.WithArguments(invocation.ArgumentList.Arguments.AddRange(newArguments));
                var newInvocation = invocation.WithArgumentList(newArgList);
                editor.GetChangedRoot();
                editor.ReplaceNode(invocation, newInvocation);
            }

            var rootWithFixedReferences = editor.GetChangedRoot();
            var localFunctionWithFixedReferences = rootWithFixedReferences.GetCurrentNode(localFunction);
            var documentWithFixedReferences = document.WithSyntaxRoot(rootWithFixedReferences);

            // Updates the declaration with the variables passed in
            var localFunctionWithFixedDeclaration = CodeGenerator.AddParameterDeclarations(
                localFunctionWithFixedReferences,
                parameters,
                documentWithFixedReferences.Project.Solution.Workspace);

            // Adds the modifier static
            var modifiers = DeclarationModifiers.From(localFunctionSymbol).WithIsStatic(true);
            var localFunctionWithStatic = (LocalFunctionStatementSyntax)CSharpSyntaxGenerator.Instance.WithModifiers(localFunctionWithFixedDeclaration, modifiers);

            var finalRoot = rootWithFixedReferences.ReplaceNode(localFunctionWithFixedReferences, localFunctionWithStatic);
            return documentWithFixedReferences.WithSyntaxRoot(finalRoot);
        }

        // Creates a new parameter symbol for all variables captured in the local function
        static ImmutableArray<IParameterSymbol> CreateParameterSymbol(ImmutableArray<ISymbol> captures)
        {
            var parameters = ArrayBuilder<IParameterSymbol>.GetInstance(captures.Length);

            foreach (var symbol in captures)
            {
                parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: RefKind.None,
                    isParams: false,
                    type: symbol.GetSymbolType(),
                    name: symbol.Name.ToCamelCase()));
            }
            return parameters.ToImmutableAndFree();
        }
    }
}
