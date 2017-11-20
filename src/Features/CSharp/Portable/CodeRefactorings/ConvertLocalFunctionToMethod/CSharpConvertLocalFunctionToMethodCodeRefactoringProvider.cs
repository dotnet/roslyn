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
                token => token.Parent is LocalFunctionStatementSyntax, cancellationToken).ConfigureAwait(false);
            if (identifier == default)
            {
                return;
            }

            var localFunction = (LocalFunctionStatementSyntax)identifier.Parent;
            if (localFunction.Identifier != identifier)
            {
                return;
            }

            if (context.Span.Length > 0 &&
                context.Span != identifier.Span)
            {
                return;
            }

            if (localFunction.ContainsDiagnostics)
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
            var capturesAsParameters = captures
                .Where(capture => !capture.IsThisParameter())
                .Select(capture => CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default,
                    refKind: dataFlow.WrittenInside.Contains(capture) ? RefKind.Ref : RefKind.None,
                    isParams: false,
                    type: capture.GetSymbolType(),
                    name: capture.Name)).ToList();

            var typeParameters = new List<ITypeParameterSymbol>();
            GetCapturedTypeParameters(declaredSymbol, typeParameters);

            // We explicitly preserve captures' types, in case they were not spelt out in the function body
            var captureTypes = captures.Select(capture => capture.GetSymbolType()).OfType<ITypeParameterSymbol>();
            RemoveUnusedTypeParameters(localFunction, semanticModel, typeParameters, reservedTypeParameters: captureTypes);

            var container = localFunction.GetAncestor<MemberDeclarationSyntax>();
            var containerSymbol = semanticModel.GetDeclaredSymbol(container, cancellationToken);
            var isStatic = containerSymbol.IsStatic || captures.All(capture => !capture.IsThisParameter());

            var methodName = GenerateUniqueMethodName(declaredSymbol);
            var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                containingType: declaredSymbol.ContainingType,
                attributes: default,
                accessibility: Accessibility.Private,
                modifiers: isStatic ? DeclarationModifiers.Static : default,
                returnType: declaredSymbol.ReturnType,
                refKind: default,
                explicitInterfaceImplementations: default,
                name: methodName,
                typeParameters: typeParameters.ToImmutableArray(),
                parameters: declaredSymbol.Parameters.AddRange(capturesAsParameters));

            var method = MethodGenerator.GenerateMethodDeclaration(methodSymbol, CodeGenerationDestination.Unspecified,
                    document.Project.Solution.Workspace, CodeGenerationOptions.Default, root.SyntaxTree.Options);

            method = WithBodyFrom(method, localFunction);

            var generator = CSharpSyntaxGenerator.Instance;
            var editor = new SyntaxEditor(root, generator);
            editor.InsertAfter(container, method);
            editor.RemoveNode(localFunction, SyntaxRemoveOptions.KeepNoTrivia);

            var needsRename = methodName != declaredSymbol.Name;
            var identifierToken = needsRename ? methodName.ToIdentifierToken() : default;
            var supportsNonTrailing = SupportsNonTrailingNamedArguments(root.SyntaxTree.Options);
            var hasAdditionalArguments = !capturesAsParameters.IsEmpty();
            var hasAdditionalTypeArguments = !typeParameters.IsEmpty();
            var additionalTypeArguments = hasAdditionalTypeArguments
                ? typeParameters.Except(declaredSymbol.TypeParameters)
                    .Select(p => (TypeSyntax)p.Name.ToIdentifierName()).ToArray()
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
                if (currentNode.Parent.IsKind(SyntaxKind.InvocationExpression, out InvocationExpressionSyntax invocation))
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

                    if (hasAdditionalTypeArguments)
                    {
                        var existingTypeArguments = symbol.TypeArguments.Select(x => x.GenerateTypeSyntax());
                        // Prepend additional type arguments to preserve lexical order in which they are defined
                        var typeArguments = additionalTypeArguments.Concat(existingTypeArguments);
                        currentNode = generator.WithTypeArguments(currentNode, typeArguments);
                    }
                }

                if (needsRename)
                {
                    currentNode = ((SimpleNameSyntax)currentNode).WithIdentifier(identifierToken);
                }

                editor.ReplaceNode(node, currentNode);
            }

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
