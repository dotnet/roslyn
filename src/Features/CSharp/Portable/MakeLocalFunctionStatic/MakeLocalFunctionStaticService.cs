// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    [Shared]
    [ExportLanguageService(typeof(MakeLocalFunctionStaticService), LanguageNames.CSharp)]
    internal sealed class MakeLocalFunctionStaticService : ILanguageService
    {
        private readonly SyntaxGenerator s_generator = CSharpSyntaxGenerator.Instance;

        internal async Task<Document> CreateParameterSymbolAsync(Document document, LocalFunctionStatementSyntax localFunction, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(true);
            var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
            var dataFlow = semanticModel.AnalyzeDataFlow(localFunction);
            var captures = dataFlow.CapturedInside;

            var parameters = CreateParameterSymbol(captures);

            //Finds all the call sites of the local function
            var workspace = document.Project.Solution.Workspace;
            var arrayNode = await SymbolFinder.FindReferencesAsync(localFunctionSymbol, document.Project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            var rootOne = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(rootOne, s_generator);

            foreach (var referenceSymbol in arrayNode)
            {
                foreach (var location in referenceSymbol.Locations)
                {
                    var root = await location.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = root.FindNode(location.Location.SourceSpan); //Node for the identifier syntax

                    var invocation = (syntaxNode as IdentifierNameSyntax).Parent as InvocationExpressionSyntax;
                    if (invocation == null)
                    {
                        return document;
                    }

                    var argList = invocation.ArgumentList;
                    List<ArgumentSyntax> paramList = new List<ArgumentSyntax>();

                    foreach (var parameter in parameters)
                    {
                        var newArgument = GenerateArgument(parameter, parameter.Name, false);
                        paramList.Add(newArgument as ArgumentSyntax);
                    }

                    var newArgList = argList.WithArguments(argList.Arguments.AddRange(paramList));
                    var newInvocation = invocation.WithArgumentList(newArgList);

                    editor.ReplaceNode(invocation, newInvocation);

                }
            }

            //Updates the declaration with the variables passed in
            var updatedLocalFunction = CodeGenerator.AddParameterDeclarations(localFunction, parameters, workspace);

            //Adds the modifier static
            var modifiers = DeclarationModifiers.From(localFunctionSymbol).WithIsStatic(true);
            var localFunctionWithStatic = s_generator.WithModifiers(updatedLocalFunction, modifiers);

            editor.ReplaceNode(localFunction, localFunctionWithStatic);

            var newRoot = editor.GetChangedRoot();
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;

            //Creates a new parameter symbol for all variables captured in the local function
            static ImmutableArray<IParameterSymbol> CreateParameterSymbol(ImmutableArray<ISymbol> captures)
            {
                var parameters = ArrayBuilder<IParameterSymbol>.GetInstance(captures.Length);

                foreach (var symbol in captures)
                {
                    var type = symbol.GetSymbolType();
                    parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: RefKind.None,
                        isParams: false,
                        type: type,
                        name: symbol.Name.ToCamelCase()));
                }

                return parameters.ToImmutableAndFree();
            }
        }

        internal SyntaxNode GenerateArgument(IParameterSymbol p, string name, bool shouldUseNamedArguments = false)
            => s_generator.Argument(shouldUseNamedArguments ? name : null, p.RefKind, name.ToIdentifierName());
    }

}

