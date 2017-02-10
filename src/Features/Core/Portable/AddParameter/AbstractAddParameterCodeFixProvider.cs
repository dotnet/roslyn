// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal abstract class AbstractAddParameterCodeFixProvider<
        TArgumentSyntax,
        TAttributeArgumentSyntax,
        TArgumentListSyntax,
        TAttributeArgumentListSyntax,
        TInvocationExpressionSyntax,
        TObjectCreationExpressionSyntax> : CodeFixProvider
        where TArgumentSyntax : SyntaxNode
        where TArgumentListSyntax : SyntaxNode
        where TAttributeArgumentListSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : SyntaxNode
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var diagnotic = context.Diagnostics.First();

            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnotic.Location.SourceSpan);

            while (node != null)
            {
                if (node is TObjectCreationExpressionSyntax objectCreation)
                {
                    await HandleObjectCreationExpressionAsync(context, objectCreation).ConfigureAwait(false);
                    return;
                }
                else if (node is TInvocationExpressionSyntax invocationExpression)
                {
                    await HandleInvocationExpressionAsync(context, invocationExpression).ConfigureAwait(false);
                    return;
                }

                node = node.Parent;
            }
        }

        private Task HandleInvocationExpressionAsync(CodeFixContext context, TInvocationExpressionSyntax invocationExpression)
        {
            return SpecializedTasks.EmptyTask;
        }

        private async Task HandleObjectCreationExpressionAsync(
            CodeFixContext context,
            TObjectCreationExpressionSyntax objectCreation)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var typeNode = syntaxFacts.GetObjectCreationType(objectCreation);
            if (typeNode == null)
            {
                return;
            }

            var type = semanticModel.GetSymbolInfo(typeNode, cancellationToken).GetAnySymbol() as INamedTypeSymbol;
            if (type == null ||
                !IsInSource(type))
            {
                return;
            }

            var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfObjectCreationExpression(objectCreation);

            var comparer = syntaxFacts.IsCaseSensitive
                ? StringComparer.Ordinal
                : CaseInsensitiveComparison.Comparer;

            foreach (var constructor in type.InstanceConstructors.OrderBy(m => m.Parameters.Length))
            {
                if (IsInSource(constructor) &&
                    constructor.Parameters.Length < arguments.Count)
                {
                    var argumentToAdd = DetermineFirstArgumentToAdd(
                        semanticModel, syntaxFacts, comparer, constructor, arguments);

                    if (argumentToAdd != null)
                    {
                        var parameters = constructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                        var signature = $"{type.Name}({string.Join(", ", parameters)})";

                        var title = string.Format(FeaturesResources.Add_parameter_to_0, signature);

                        context.RegisterCodeFix(
                             new MyCodeAction(title, c => FixAsync(document, constructor, argumentToAdd, arguments, c)),
                             context.Diagnostics);
                    }
                }
            }
        }

        private bool IsInSource(ISymbol symbol)
            => !symbol.IsImplicitlyDeclared &&
               symbol.DeclaringSyntaxReferences.Length > 0 &&
               symbol.Locations.Length > 0 && 
               symbol.Locations.All(loc => loc.IsInSource);

        private async Task<Document> FixAsync(
            Document invocationDocument, 
            IMethodSymbol method,
            TArgumentSyntax argument,
            SeparatedSyntaxList<TArgumentSyntax> argumentList,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(invocationDocument.Project.Solution.Workspace, method.Language);

            var methodDeclaration = await method.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = invocationDocument.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = invocationDocument.GetLanguageService<ISemanticFactsService>();
            var argumentName = syntaxFacts.GetNameForArgument(argument);
            var expression = syntaxFacts.GetExpressionOfArgument(argument);

            var semanticModel = await invocationDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var parameterType = semanticModel.GetTypeInfo(expression).Type ?? semanticModel.Compilation.ObjectType;

            var newMethodDeclaration = GetNewMethodDeclaration(
                method, argument, argumentList, generator, methodDeclaration, 
                semanticFacts, argumentName, expression, semanticModel, parameterType);

            var root = methodDeclaration.SyntaxTree.GetRoot(cancellationToken);
            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

            var methodDocument = invocationDocument.Project.Solution.GetDocument(methodDeclaration.SyntaxTree);
            var newDocument = methodDocument.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private static SyntaxNode GetNewMethodDeclaration(IMethodSymbol method, TArgumentSyntax argument, SeparatedSyntaxList<TArgumentSyntax> argumentList, SyntaxGenerator generator, SyntaxNode declaration, ISemanticFactsService semanticFacts, string argumentName, SyntaxNode expression, SemanticModel semanticModel, ITypeSymbol parameterType)
        {
            if (!string.IsNullOrWhiteSpace(argumentName))
            {
                var newParameterSymbol = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    null, RefKind.None,
                    isParams: false,
                    type: parameterType,
                    name: argumentName);

                var newParameterDeclaration = generator.ParameterDeclaration(newParameterSymbol);
                return generator.AddParameters(declaration, new[] { newParameterDeclaration });
            }
            else
            {
                var name = semanticFacts.GenerateNameForExpression(semanticModel, expression);
                var uniqueName = NameGenerator.EnsureUniqueness(name, method.Parameters.Select(p => p.Name));

                var newParameterSymbol = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    null, RefKind.None,
                    isParams: false,
                    type: parameterType,
                    name: uniqueName);

                var argumentIndex = argumentList.IndexOf(argument);
                var newParameterDeclaration = generator.ParameterDeclaration(newParameterSymbol);
                return generator.InsertParameters(
                    declaration, argumentIndex, new[] { newParameterDeclaration });
            }
        }

        private static readonly SymbolDisplayFormat SimpleFormat =
                    new SymbolDisplayFormat(
                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                        parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private TArgumentSyntax DetermineFirstArgumentToAdd(
                SemanticModel semanticModel,
                ISyntaxFactsService syntaxFacts,
                StringComparer comparer,
                IMethodSymbol method,
                SeparatedSyntaxList<TArgumentSyntax> arguments)
        {
            var methodParameterNames = new HashSet<string>(comparer);
            methodParameterNames.AddRange(method.Parameters.Select(p => p.Name));

            for (int i = 0, n = arguments.Count; i < n; i++)
            {
                var argument = arguments[i];
                var argumentName = syntaxFacts.GetNameForArgument(argument);

                if (!string.IsNullOrWhiteSpace(argumentName))
                {
                    // If the user provided an argument-name and we don't have any parameters that
                    // match, then this is the argument we want to add a parameter for.
                    if (!methodParameterNames.Contains(argumentName))
                    {
                        return argument;
                    }
                }
                else
                {
                    // Positional argument.  If the position is beyond what the method supports,
                    // then this definitely is an argument we could add.
                    if (i >= method.Parameters.Length)
                    {
                        return argument;
                    }

                    var argumentTypeInfo = semanticModel.GetTypeInfo(syntaxFacts.GetExpressionOfArgument(argument));
                    var parameter = method.Parameters[i];

                    // If this argument already matches the method's parameter, then we don't want 
                    // to add it.
                    if (!parameter.Type.Equals(argumentTypeInfo.Type) &&
                        !parameter.Type.Equals(argumentTypeInfo.ConvertedType))
                    {
                        return argument;
                    }
                }
            }

            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}