// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertLocalFunctionToMethodCodeRefactoringProvider)), Shared]
    internal sealed class CSharpConvertLocalFunctionToMethodCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var identifier = await root.SyntaxTree.GetTouchingTokenAsync(context.Span.Start,
                token => token.Parent.IsKind(SyntaxKind.LocalFunctionStatement), cancellationToken).ConfigureAwait(false);
            if (identifier == default)
            {
                return;
            }

            if (context.Span.Length > 0 &&
                context.Span != identifier.Span)
            {
                return;
            }

            var localFunction = (LocalFunctionStatementSyntax)identifier.Parent;
            if (localFunction.Identifier != identifier)
            {
                return;
            }

            if (!localFunction.Parent.IsKind(SyntaxKind.Block, out BlockSyntax parentBlock))
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(c => UpdateDocumentAsync(root, document, parentBlock, localFunction, c)));
        }

        private static async Task<Document> UpdateDocumentAsync(
            SyntaxNode root,
            Document document,
            BlockSyntax parentBlock,
            LocalFunctionStatementSyntax localFunction,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declaredSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);

            var dataFlow = semanticModel.AnalyzeDataFlow(
                localFunction.Body ?? (SyntaxNode)localFunction.ExpressionBody.Expression);
            var captures = dataFlow.Captured;

            // First, create a parameter per each capture so that we can pass them as arguments to the final method
            // Filter out `this` because it doesn't need a parameter, we will just make a non-static method for that
            // We also make a `ref` parameter here for each capture that is being written into inside the funciton
            var capturesAsParameters = captures
                .Where(capture => !capture.IsThisParameter())
                .Select(capture => CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: dataFlow.WrittenInside.Contains(capture) ? RefKind.Ref : RefKind.None,
                    isParams: false,
                    type: capture.GetSymbolType(),
                    name: capture.Name)).ToList();

            // Find all enclosing type parameters e.g. from outer local functions and the containing member
            // We exclude the containing type itself which has type parameters accessible to all members
            var typeParameters = new List<ITypeParameterSymbol>();
            GetCapturedTypeParameters(declaredSymbol, typeParameters);

            // We're going to remove unreferenced type parameters but we explicitly preserve
            // captures' types, just in case that they were not spelt out in the function body
            var captureTypes = captures.SelectMany(capture => capture.GetSymbolType().GetReferencedTypeParameters());
            RemoveUnusedTypeParameters(localFunction, semanticModel, typeParameters, reservedTypeParameters: captureTypes);

            var container = localFunction.GetAncestor<MemberDeclarationSyntax>();
            var containerSymbol = semanticModel.GetDeclaredSymbol(container, cancellationToken);
            var isStatic = containerSymbol.IsStatic || captures.All(capture => !capture.IsThisParameter());

            var methodName = GenerateUniqueMethodName(declaredSymbol);
            var parameters = declaredSymbol.Parameters;
            var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                containingType: declaredSymbol.ContainingType,
                attributes: default,
                accessibility: Accessibility.Private,
                modifiers: new DeclarationModifiers(isStatic, isAsync: declaredSymbol.IsAsync),
                returnType: declaredSymbol.ReturnType,
                refKind: default,
                explicitInterfaceImplementations: default,
                name: methodName,
                typeParameters: typeParameters.ToImmutableArray(),
                parameters: parameters.AddRange(capturesAsParameters));

            var defaultOptions = CodeGenerationOptions.Default;
            var method = MethodGenerator.GenerateMethodDeclaration(methodSymbol, CodeGenerationDestination.Unspecified,
                    document.Project.Solution.Workspace, defaultOptions, root.SyntaxTree.Options);

            var generator = CSharpSyntaxGenerator.Instance;
            var editor = new SyntaxEditor(root, generator);

            var needsRename = methodName != declaredSymbol.Name;
            var identifierToken = needsRename ? methodName.ToIdentifierToken() : default;
            var supportsNonTrailing = SupportsNonTrailingNamedArguments(root.SyntaxTree.Options);
            var hasAdditionalArguments = !capturesAsParameters.IsEmpty();
            var additionalTypeParameters = typeParameters.Except(declaredSymbol.TypeParameters).ToList();
            var hasAdditionalTypeArguments = !additionalTypeParameters.IsEmpty();
            var additionalTypeArguments = hasAdditionalTypeArguments
                ? additionalTypeParameters.Select(p => (TypeSyntax)p.Name.ToIdentifierName()).ToArray()
                : null;

            // Update callers' name, arguments and type arguments
            foreach (var node in parentBlock.DescendantNodes())
            {
                // A local function reference can only be an identifier or a generic name.
                switch (node.Kind())
                {
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        break;
                    default:
                        continue;
                }

                // Using symbol to get type arguments, since it could be inferred and not present in the source
                var symbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol as IMethodSymbol;
                if (symbol?.OriginalDefinition != declaredSymbol)
                {
                    continue;
                }

                var currentNode = node;

                if (needsRename)
                {
                    currentNode = ((SimpleNameSyntax)currentNode).WithIdentifier(identifierToken);
                }

                if (hasAdditionalTypeArguments)
                {
                    var existingTypeArguments = symbol.TypeArguments.Select(x => x.GenerateTypeSyntax());
                    // Prepend additional type arguments to preserve lexical order in which they are defined
                    var typeArguments = additionalTypeArguments.Concat(existingTypeArguments);
                    currentNode = generator.WithTypeArguments(currentNode, typeArguments);
                    currentNode = currentNode.WithAdditionalAnnotations(Simplifier.Annotation);
                }

                if (node.Parent.IsKind(SyntaxKind.InvocationExpression, out InvocationExpressionSyntax invocation))
                {
                    if (hasAdditionalArguments)
                    {
                        var shouldUseNamedArguments =
                            !supportsNonTrailing && invocation.ArgumentList.Arguments.Any(arg => arg.NameColon != null);

                        var additionalArguments = capturesAsParameters.Select(parameter =>
                            (ArgumentSyntax)generator.Argument(
                                name: shouldUseNamedArguments ? parameter.Name : null,
                                refKind: parameter.RefKind,
                                expression: parameter.Name.ToIdentifierName())).ToArray();

                        editor.ReplaceNode(invocation.ArgumentList,
                            invocation.ArgumentList.AddArguments(additionalArguments));
                    }
                }
                else if (hasAdditionalArguments || hasAdditionalTypeArguments)
                {
                    // Convert local function delegates to lambda if the signature no longer matches
                    var arguments = parameters
                        .Concat(capturesAsParameters)
                        .Select(parameter => generator.Argument(
                            name: null, refKind: parameter.RefKind,
                            expression: parameter.Name.ToIdentifierName()));

                    currentNode = generator.ValueReturningLambdaExpression(
                        lambdaParameters: ParameterGenerator.GetParameters(parameters, isExplicit: true, options: defaultOptions),
                        expression: generator.InvocationExpression(currentNode, arguments));

                    currentNode = currentNode.WithAdditionalAnnotations(Simplifier.Annotation);

                    if (node.IsParentKind(SyntaxKind.CastExpression))
                    {
                        currentNode = ((ExpressionSyntax)currentNode).Parenthesize();
                    }
                }

                editor.ReplaceNode(node, currentNode);
            }

            editor.TrackNode(localFunction);
            editor.TrackNode(container);

            root = editor.GetChangedRoot();

            localFunction = root.GetCurrentNode(localFunction);
            container = root.GetCurrentNode(container);

            method = WithBodyFrom(method, localFunction);

            editor = new SyntaxEditor(root, generator);
            editor.InsertAfter(container, method);
            editor.RemoveNode(localFunction, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private static bool SupportsNonTrailingNamedArguments(ParseOptions options)
            => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7_2;

        private static MethodDeclarationSyntax WithBodyFrom(
            MethodDeclarationSyntax method, LocalFunctionStatementSyntax localFunction)
        {
            return method
                .WithExpressionBody(localFunction.ExpressionBody)
                .WithSemicolonToken(localFunction.SemicolonToken)
                .WithBody(localFunction.Body);
        }

        private static void GetCapturedTypeParameters(ISymbol symbol, List<ITypeParameterSymbol> typeParameters)
        {
            var containingSymbol = symbol.ContainingSymbol;
            if (containingSymbol != null &&
                containingSymbol.Kind != SymbolKind.NamedType)
            {
                GetCapturedTypeParameters(containingSymbol, typeParameters);
            }

            typeParameters.AddRange(symbol.GetTypeParameters());
        }

        private static void RemoveUnusedTypeParameters(
            SyntaxNode localFunction,
            SemanticModel semanticModel,
            List<ITypeParameterSymbol> typeParameters,
            IEnumerable<ITypeParameterSymbol> reservedTypeParameters)
        {
            var unusedTypeParameters = typeParameters.ToList();
            foreach (var id in localFunction.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var symbol = semanticModel.GetSymbolInfo(id).Symbol;
                if (symbol != null && symbol.OriginalDefinition is ITypeParameterSymbol typeParameter)
                {
                    unusedTypeParameters.Remove(typeParameter);
                }
            }

            typeParameters.RemoveRange(unusedTypeParameters.Except(reservedTypeParameters));
        }

        private static string GenerateUniqueMethodName(ISymbol declaredSymbol)
        {
            return NameGenerator.EnsureUniqueness(
                baseName: declaredSymbol.Name,
                reservedNames: declaredSymbol.ContainingType.GetMembers().Select(m => m.Name));
        }

        private sealed class MyCodeAction : CodeActions.CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Convert_to_regular_method, createChangedDocument)
            {
            }
        }
    }
}
