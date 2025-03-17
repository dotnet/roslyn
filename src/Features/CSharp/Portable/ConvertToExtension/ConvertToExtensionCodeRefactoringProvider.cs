// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToExtension), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ConvertToExtensionCodeRefactoringProvider() : CodeRefactoringProvider
{
    private readonly record struct ExtensionMethodInfo(
        MethodDeclarationSyntax ExtensionMethod,
        IParameterSymbol FirstParameter,
        ImmutableArray<ITypeParameterSymbol> MethodTypeParameters);

    internal override FixAllProvider? GetFixAllProvider()
        => new ConvertToExtensionFixAllProvider();

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        // If the user is on an extension method, offer to convert it to a modern extension.
        var methodDeclaration = await context.TryGetRelevantNodeAsync<MethodDeclarationSyntax>().ConfigureAwait(false);
        if (methodDeclaration != null)
        {
            if (IsExtensionMethod(methodDeclaration, out var classDeclaration))
            {
                await ComputeRefactoringsAsync(
                    context, classDeclaration, [methodDeclaration],
                    CSharpFeaturesResources.Convert_extension_method_to_extension,
                    nameof(CSharpFeaturesResources.Convert_extension_method_to_extension)).ConfigureAwait(false);
            }

            return;
        }
        else
        {
            // Otherwise, if they're on a static class, which contains extension methods, offer to convert all of them.
            var classDeclaration = await context.TryGetRelevantNodeAsync<ClassDeclarationSyntax>().ConfigureAwait(false);
            if (classDeclaration != null)
            {
                await ComputeRefactoringsAsync(
                    context, classDeclaration, GetExtensionMethods(classDeclaration),
                    CSharpFeaturesResources.Convert_all_extension_methods_to_extension,
                    nameof(CSharpFeaturesResources.Convert_all_extension_methods_to_extension)).ConfigureAwait(false);
            }
        }
    }

    private static bool IsExtensionMethod(
        MethodDeclarationSyntax methodDeclaration,
        [NotNullWhen(true)] out ClassDeclarationSyntax? classDeclaration)
    {
        classDeclaration = null;
        if (methodDeclaration.ParameterList.Parameters is not [var firstParameter, ..])
            return false;

        if (!firstParameter.Modifiers.Any(SyntaxKind.ThisKeyword))
            return false;

        classDeclaration = methodDeclaration.Parent as ClassDeclarationSyntax;
        return classDeclaration != null;
    }

    private static ImmutableArray<MethodDeclarationSyntax> GetExtensionMethods(ClassDeclarationSyntax classDeclaration)
        => classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) && classDeclaration.Parent is BaseNamespaceDeclarationSyntax
            ? [.. classDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(m => IsExtensionMethod(m, out _))]
            : [];

    private async Task ComputeRefactoringsAsync(
        CodeRefactoringContext context,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<MethodDeclarationSyntax> extensionMethods,
        string title,
        string equivalenceKey)
    {
        if (extensionMethods.IsEmpty)
            return;

        context.RegisterRefactoring(CodeAction.Create(
            title,
            cancellationToken => ConvertToExtensionAsync(context.Document, classDeclaration, extensionMethods, cancellationToken),
            equivalenceKey));
    }

    private async Task<Document> ConvertToExtensionAsync(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<MethodDeclarationSyntax> extensionMethods,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(extensionMethods.IsEmpty);

        var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var newDeclaration = await ConvertToExtensionAsync(
            codeGenerationService, semanticModel, classDeclaration, extensionMethods, cancellationToken).ConfigureAwait(false);

        var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Core function that the normal fix and the fix-all-provider call into to fixup one class declaration and the set
    /// of desired extension methods within that class declaration.  When called on an extension method itself, this
    /// will just be one extension method.  When called on a class declaration, this will be all the extension methods
    /// in that class.
    /// </summary>
    private static async Task<ClassDeclarationSyntax> ConvertToExtensionAsync(
        ICodeGenerationService codeGenerationService,
        SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<MethodDeclarationSyntax> extensionMethods,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(extensionMethods.IsEmpty);

        // Group extension methods as long as their first-parameter is compatible.
        var extensionMethodInfos = extensionMethods
            .Select(extensionMethod =>
            {
                var firstParameter = semanticModel.GetRequiredDeclaredSymbol(extensionMethod.ParameterList.Parameters[0], cancellationToken);
                using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var methodTypeParameters);
                return new ExtensionMethodInfo(
                    extensionMethod, firstParameter, [.. methodTypeParameters.OrderBy(t => t.Name)]);
            });

        var groups = extensionMethodInfos.GroupBy(x => x, ExtensionMethodEqualityComparer.Instance);

        // Process all the groups, ordered by the first extension method's start position.  That way each group of extensions
        // is merged into a final extension that will go into that location in the original class declaration.
        var classDeclarationEditor = new SyntaxEditor(classDeclaration, CSharpSyntaxGenerator.Instance);
        foreach (var group in groups.OrderBy(g => g.Min(info => info.ExtensionMethod.SpanStart)))
        {
            var newExtension = CreateExtension([.. group]);

            classDeclarationEditor.ReplaceNode(group.First().ExtensionMethod, newExtension);

            foreach (var extensionMethod in group.Skip(1))
                classDeclarationEditor.RemoveNode(extensionMethod.ExtensionMethod);
        }

        return (ClassDeclarationSyntax)classDeclarationEditor.GetChangedRoot();

        ExtensionDeclarationSyntax CreateExtension(ImmutableArray<ExtensionMethodInfo> group)
        {
            Contract.ThrowIfTrue(group.IsEmpty);

            var codeGenerationInfo = new CSharpCodeGenerationContextInfo(
                CodeGenerationContext.Default,
                CSharpCodeGenerationOptions.Default,
                (CSharpCodeGenerationService)codeGenerationService,
                LanguageVersionExtensions.CSharpNext);

            var firstExtensionInfo = group[0];
            var typeParameters = firstExtensionInfo.MethodTypeParameters.CastArray<ITypeParameterSymbol>();

            // Create a disconnected parameter.  This way when we look at it, we won't think of it as an extension method
            // parameter any more.  This will prevent us from undesirable things (like placing 'this' on it when adding to
            // the extension declaration).
            var firstParameter = CodeGenerationSymbolFactory.CreateParameterSymbol(firstExtensionInfo.FirstParameter);

            var extensionDeclaration = ExtensionDeclaration()
                .WithTypeParameterList(TypeParameterGenerator.GenerateTypeParameterList(typeParameters, codeGenerationInfo))
                .WithConstraintClauses(typeParameters.GenerateConstraintClauses())
                .WithParameterList(ParameterGenerator.GenerateParameterList([firstParameter], isExplicit: false, codeGenerationInfo))
                .WithMembers([.. group.Select(ConvertExtensionMethod)]);

            // Move the blank lines above the first extension method inside the extension to the extension itself.
            firstExtensionInfo.ExtensionMethod.GetNodeWithoutLeadingBlankLines(out var leadingBlankLines);
            return extensionDeclaration.WithLeadingTrivia(leadingBlankLines);
        }

        MethodDeclarationSyntax ConvertExtensionMethod(
            ExtensionMethodInfo extensionMethodInfo, int index)
        {
            using var _ = PooledHashSet<string>.GetInstance(out var typeParametersToRemove);

            var converted = extensionMethodInfo.ExtensionMethod
                .WithParameterList(ConvertParameters(extensionMethodInfo))
                .WithTypeParameterList(ConvertTypeParameters(extensionMethodInfo, typeParametersToRemove))
                .WithConstraintClauses(ConvertConstraintClauses(extensionMethodInfo, typeParametersToRemove));

            if (index == 0)
                converted = converted.GetNodeWithoutLeadingBlankLines();

            // Note: Formatting in this fashion is not desirable.  Ideally we would use
            // https://github.com/dotnet/roslyn/issues/59228 to just attach an indentation annotation to the extension
            // method to indent it instead.
            return converted.WithAdditionalAnnotations(Formatter.Annotation);
        }
    }

    private static ParameterListSyntax ConvertParameters(ExtensionMethodInfo extensionMethodInfo)
    {
        var extensionMethod = extensionMethodInfo.ExtensionMethod;

        // skip the first parameter, which is the 'this' parameter, and the comma that follows it.
        return extensionMethod.ParameterList.WithParameters(
            SeparatedList<ParameterSyntax>(extensionMethodInfo.ExtensionMethod.ParameterList.Parameters.GetWithSeparators().Skip(2)));
    }

    private static TypeParameterListSyntax? ConvertTypeParameters(
        ExtensionMethodInfo extensionMethodInfo,
        HashSet<string> typeParametersToRemove)
    {
        var extensionMethod = extensionMethodInfo.ExtensionMethod;

        // If the extension method wasn't generic, or we're not removing any type parameters, there's nothing to do.
        if (extensionMethod.TypeParameterList is null || typeParametersToRemove.Count == 0)
            return extensionMethod.TypeParameterList;

        // If we're removing all the type parameters, remove the type parameter list entirely.
        if (typeParametersToRemove.Count == extensionMethod.TypeParameterList.Parameters.Count)
            return null;

        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var newTypeParameters);
        var nodesAndTokens = extensionMethod.TypeParameterList.Parameters.GetWithSeparators();

        for (var i = 0; i < nodesAndTokens.Count; i += 2)
        {
            var typeParameter = (TypeParameterSyntax)nodesAndTokens[i]!;
            if (typeParametersToRemove.Contains(typeParameter.Identifier.ValueText))
                continue;

            // Add preceding comma if needed.  Note: this will always succeed as we can only have a prior
            // newTypeParameter if we hit a type parameter before us that we want to keep.
            if (newTypeParameters.Count > 0)
                newTypeParameters.Add(nodesAndTokens[i - 1]);

            newTypeParameters.Add(typeParameter);
        }

        return extensionMethod.TypeParameterList.WithParameters(SeparatedList<TypeParameterSyntax>(newTypeParameters));
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> ConvertConstraintClauses(ExtensionMethodInfo extensionMethodInfo)
    {
        throw new NotImplementedException();
    }

}
