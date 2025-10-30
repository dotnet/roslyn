// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
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

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertLocalFunctionToMethod), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpConvertLocalFunctionToMethodCodeRefactoringProvider() : CodeRefactoringProvider
{
    private static readonly SyntaxAnnotation s_delegateToReplaceAnnotation = new();

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
            return;

        var localFunction = await context.TryGetRelevantNodeAsync<LocalFunctionStatementSyntax>().ConfigureAwait(false);
        if (localFunction?.Parent is not BlockSyntax parentBlock)
            return;

        var container = localFunction.GetAncestor<MemberDeclarationSyntax>();

        // If the local function is defined in a block within the top-level statements context, then we can't provide the refactoring because
        // there is no class we can put the generated method in.
        if (container is null or GlobalStatementSyntax or FieldDeclarationSyntax or EventFieldDeclarationSyntax)
            return;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var containerSymbol = semanticModel.GetDeclaredSymbol(container);
        if (containerSymbol is null)
            return;

        context.RegisterRefactoring(
            CodeAction.Create(
                CSharpFeaturesResources.Convert_to_method,
                cancellationToken => UpdateDocumentAsync(document, parentBlock, localFunction, container, cancellationToken),
                nameof(CSharpFeaturesResources.Convert_to_method)),
            localFunction.Span);
    }

    private static async Task<Document> UpdateDocumentAsync(
        Document document,
        BlockSyntax parentBlock,
        LocalFunctionStatementSyntax localFunction,
        MemberDeclarationSyntax container,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var declaredSymbol = semanticModel.GetRequiredDeclaredSymbol(localFunction, cancellationToken);

        Contract.ThrowIfTrue(localFunction is { Body: null, ExpressionBody: null });

        var dataFlow = semanticModel.AnalyzeDataFlow(
            localFunction.Body ?? (SyntaxNode)localFunction.ExpressionBody!.Expression);

        // Exclude local function parameters in case they were captured inside the function body
        var captures = dataFlow.CapturedInside.Except(dataFlow.VariablesDeclared).Except(declaredSymbol.Parameters).ToImmutableArray();

        // First, create a parameter per each capture so that we can pass them as arguments to the final method
        // Filter out `this` because it doesn't need a parameter, we will just make a non-static method for that
        // We also make a `ref` parameter here for each capture that is being written into inside the function
        var capturesAsParameters = captures.SelectAsArray(
            capture => !capture.IsThisParameter(),
            capture => CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default,
                refKind: dataFlow.WrittenInside.Contains(capture) ? RefKind.Ref : RefKind.None,
                isParams: false,
                type: capture.GetSymbolType() ?? semanticModel.Compilation.ObjectType,
                name: capture.Name));

        // Find all enclosing type parameters e.g. from outer local functions and the containing member
        // We exclude the containing type itself which has type parameters accessible to all members
        var typeParameters = CreateFinalTypeParameterList(semanticModel, localFunction, declaredSymbol, captures);

        var containerSymbol = semanticModel.GetRequiredDeclaredSymbol(container, cancellationToken);
        var isStatic = containerSymbol.IsStatic || captures.All(capture => !capture.IsThisParameter());

        // GetSymbolModifiers actually checks if the local function needs to be unsafe, not whether
        // it is declared as such, so this check we don't need to worry about whether the containing method
        // is unsafe, this will just work regardless.
        var needsUnsafe = declaredSymbol.GetSymbolModifiers().IsUnsafe;

        var methodName = GenerateUniqueMethodName(declaredSymbol);
        var parameters = declaredSymbol.Parameters;
        var finalParameterList = CreateFinalParameterList(parameters, capturesAsParameters);

        var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
            containingType: declaredSymbol.ContainingType,
            attributes: default,
            accessibility: Accessibility.Private,
            modifiers: new DeclarationModifiers(isStatic, isAsync: declaredSymbol.IsAsync, isUnsafe: needsUnsafe),
            returnType: declaredSymbol.ReturnType,
            refKind: declaredSymbol.RefKind,
            explicitInterfaceImplementations: default,
            name: methodName,
            typeParameters: [.. typeParameters],
            parameters: finalParameterList);

        var info = (CSharpCodeGenerationContextInfo)await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(false);
        var method = MethodGenerator.GenerateMethodDeclaration(methodSymbol, CodeGenerationDestination.Unspecified, info, cancellationToken);

        if (localFunction.AttributeLists.Count > 0)
            method = method.WithoutLeadingTrivia().WithAttributeLists(localFunction.AttributeLists).WithLeadingTrivia(method.GetLeadingTrivia());

        var generator = CSharpSyntaxGenerator.Instance;
        var editor = new SyntaxEditor(root, generator);

        var needsRename = methodName != declaredSymbol.Name;
        var identifierToken = needsRename ? methodName.ToIdentifierToken() : default;
        var supportsNonTrailing = SupportsNonTrailingNamedArguments(root.SyntaxTree.Options);
        var hasAdditionalArguments = capturesAsParameters.Length != 0;
        var additionalTypeParameters = typeParameters.Except(declaredSymbol.TypeParameters).ToImmutableArray();
        var hasAdditionalTypeArguments = additionalTypeParameters.Length != 0;
        var additionalTypeArguments = additionalTypeParameters.SelectAsArray(p => (TypeSyntax)p.Name.ToIdentifierName());

        var anyDelegatesToReplace = false;
        // Update callers' name, arguments and type arguments
        // A local function reference can only be an identifier or a generic name.
        foreach (var simpleName in parentBlock.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            // Using symbol to get type arguments, since it could be inferred and not present in the source
            var symbol = semanticModel.GetSymbolInfo(simpleName, cancellationToken).Symbol as IMethodSymbol;
            if (!Equals(symbol?.OriginalDefinition, declaredSymbol))
            {
                continue;
            }

            var currentNode = simpleName;
            if (needsRename)
                currentNode = currentNode.WithIdentifier(identifierToken).WithTriviaFrom(currentNode);

            if (hasAdditionalTypeArguments)
            {
                // Prepend additional type arguments to preserve lexical order in which they are defined
                var typeArguments = additionalTypeArguments.Concat(symbol.TypeArguments.Select(s => s.GenerateTypeSyntax()));
                currentNode = (SimpleNameSyntax)generator
                    .WithTypeArguments(currentNode, typeArguments)
                    .WithAdditionalAnnotations(Simplifier.Annotation);
            }

            if (simpleName.Parent is InvocationExpressionSyntax invocation)
            {
                if (hasAdditionalArguments)
                {
                    var firstOptionalOrParamsArgument = invocation.ArgumentList.Arguments.FirstOrDefault(
                        a =>
                        {
                            var parameter = a.DetermineParameter(semanticModel, allowUncertainCandidates: true, allowParams: true, cancellationToken);
                            if (parameter is null)
                                return false;

                            return parameter.IsOptional || parameter.IsParams;
                        });

                    // Attempt to place the new arguments appropriately in the original invocation, accounting for things
                    // like optional/params args. Otherwise, just fallback to appending the new arguments at the end if we
                    // can't get proper semantics here.
                    var insertionIndex = firstOptionalOrParamsArgument != null
                        ? invocation.ArgumentList.Arguments.IndexOf(firstOptionalOrParamsArgument)
                        : invocation.ArgumentList.Arguments.Count;

                    var shouldUseNamedArguments =
                        !supportsNonTrailing && invocation.ArgumentList.Arguments.Take(insertionIndex).Any(arg => arg.NameColon != null);

                    var additionalArguments = capturesAsParameters.Select(p =>
                        (ArgumentSyntax)GenerateArgument(p, p.Name, shouldUseNamedArguments)).ToArray();

                    editor.ReplaceNode(
                        invocation.ArgumentList,
                        invocation.ArgumentList.WithArguments(
                            invocation.ArgumentList.Arguments.InsertRange(insertionIndex, additionalArguments)));
                }
            }
            else if (hasAdditionalArguments || hasAdditionalTypeArguments)
            {
                // Convert local function delegates to lambda if the signature no longer matches
                currentNode = currentNode.WithAdditionalAnnotations(s_delegateToReplaceAnnotation);
                anyDelegatesToReplace = true;
            }

            editor.ReplaceNode(simpleName, currentNode);
        }

        editor.TrackNode(localFunction);
        editor.TrackNode(container);

        root = editor.GetChangedRoot();

        localFunction = root.GetCurrentNode(localFunction) ?? throw ExceptionUtilities.Unreachable();
        container = root.GetCurrentNode(container) ?? throw ExceptionUtilities.Unreachable();

        method = WithBodyFrom(method, localFunction);

        editor = new SyntaxEditor(root, generator);
        editor.InsertAfter(container, method);
        editor.RemoveNode(localFunction, SyntaxRemoveOptions.KeepNoTrivia);

        if (anyDelegatesToReplace)
        {
            document = document.WithSyntaxRoot(editor.GetChangedRoot());
            semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            editor = new SyntaxEditor(root, generator);

            foreach (var node in root.GetAnnotatedNodes(s_delegateToReplaceAnnotation))
            {
                var reservedNames = GetReservedNames(node, semanticModel, cancellationToken);
                var parameterNames = GenerateUniqueParameterNames(parameters, reservedNames);
                var lambdaParameters = parameters.Zip(parameterNames, (p, name) => GenerateParameter(p, name));
                var lambdaArguments = parameters.Zip(parameterNames, (p, name) => GenerateArgument(p, name));
                var additionalArguments = capturesAsParameters.Select(p => GenerateArgument(p, p.Name));
                var newNode = generator.ValueReturningLambdaExpression(lambdaParameters,
                    generator.InvocationExpression(node, lambdaArguments.Concat(additionalArguments)));

                newNode = newNode.WithAdditionalAnnotations(Simplifier.Annotation);

                if (node.IsParentKind(SyntaxKind.CastExpression))
                {
                    newNode = ((ExpressionSyntax)newNode).Parenthesize();
                }

                editor.ReplaceNode(node, newNode);
            }
        }

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static ImmutableArray<ITypeParameterSymbol> CreateFinalTypeParameterList(
        SemanticModel semanticModel,
        LocalFunctionStatementSyntax localFunction,
        IMethodSymbol declaredSymbol,
        ImmutableArray<ISymbol> captures)
    {
        var typeParameters = new List<ITypeParameterSymbol>();
        for (var containingMethod = declaredSymbol; containingMethod != null; containingMethod = containingMethod.ContainingSymbol as IMethodSymbol)
            typeParameters.InsertRange(0, containingMethod.GetTypeParameters());

        // We're going to remove unreferenced type parameters but we explicitly preserve
        // captures' types, just in case that they were not spelt out in the function body
        var reservedTypeParameters = captures.SelectMany(capture => capture.GetSymbolType().GetReferencedTypeParameters());
        var unusedTypeParameters = typeParameters.ToList();
        foreach (var id in localFunction.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var symbol = semanticModel.GetSymbolInfo(id).Symbol;
            if (symbol?.OriginalDefinition is ITypeParameterSymbol typeParameter)
                unusedTypeParameters.Remove(typeParameter);
        }

        typeParameters.RemoveRange(unusedTypeParameters.Except(reservedTypeParameters));
        return [.. typeParameters];
    }

    private static ImmutableArray<IParameterSymbol> CreateFinalParameterList(
        ImmutableArray<IParameterSymbol> parameters, ImmutableArray<IParameterSymbol> capturesAsParameters)
    {
        var firstOptionalOrParamsParameterIndex = parameters.IndexOf(p => p.IsOptional || p.IsParams);
        if (firstOptionalOrParamsParameterIndex < 0)
            firstOptionalOrParamsParameterIndex = parameters.Length;

        return [.. parameters.Take(firstOptionalOrParamsParameterIndex), .. capturesAsParameters, .. parameters.Skip(firstOptionalOrParamsParameterIndex)];
    }

    private static bool SupportsNonTrailingNamedArguments(ParseOptions options)
        => options.LanguageVersion() >= LanguageVersion.CSharp7_2;

    private static SyntaxNode GenerateArgument(IParameterSymbol p, string name, bool shouldUseNamedArguments = false)
        => CSharpSyntaxGenerator.Instance.Argument(shouldUseNamedArguments ? name : null, p.RefKind, name.ToIdentifierName());

    private static ImmutableArray<string> GenerateUniqueParameterNames(ImmutableArray<IParameterSymbol> parameters, ImmutableArray<string> reservedNames)
        => [.. parameters.Select(p => NameGenerator.EnsureUniqueness(p.Name, reservedNames))];

    private static ImmutableArray<string> GetReservedNames(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        => [.. semanticModel.GetAllDeclaredSymbols(node.GetAncestor<MemberDeclarationSyntax>(), cancellationToken).Select(s => s.Name)];

    private static ParameterSyntax GenerateParameter(IParameterSymbol parameter, string name)
    {
        return SyntaxFactory.Parameter(name.ToIdentifierToken())
            .WithModifiers(CSharpSyntaxGeneratorInternal.GetParameterModifiers(parameter))
            .WithType(parameter.Type.GenerateTypeSyntax());
    }

    private static MethodDeclarationSyntax WithBodyFrom(
        MethodDeclarationSyntax method, LocalFunctionStatementSyntax localFunction)
    {
        return method
            .WithExpressionBody(localFunction.ExpressionBody)
            .WithSemicolonToken(localFunction.SemicolonToken)
            .WithBody(localFunction.Body);
    }

    private static string GenerateUniqueMethodName(ISymbol declaredSymbol)
    {
        return NameGenerator.EnsureUniqueness(
            baseName: declaredSymbol.Name,
            reservedNames: declaredSymbol.ContainingType.GetMembers().Select(m => m.Name));
    }
}
