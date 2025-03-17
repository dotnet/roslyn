// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            var newExtension = CreateExtension(codeGenerationService, [.. group]);
            classDeclarationEditor.ReplaceNode(group.First().ExtensionMethod, newExtension);

            foreach (var extensionMethod in group.Skip(1))
                classDeclarationEditor.RemoveNode(extensionMethod.ExtensionMethod);
        }

        return (ClassDeclarationSyntax)classDeclarationEditor.GetChangedRoot();
    }

    private static ExtensionDeclarationSyntax CreateExtension(
        ICodeGenerationService codeGenerationService, ImmutableArray<ExtensionMethodInfo> group)
    {
        Contract.ThrowIfTrue(group.IsEmpty);

        var codeGenerationInfo = new CSharpCodeGenerationContextInfo(
            CodeGenerationContext.Default,
            CSharpCodeGenerationOptions.Default,
            (CSharpCodeGenerationService)codeGenerationService,
            LanguageVersionExtensions.CSharpNext);

        var firstExtensionInfo = group[0];
        var typeParameters = firstExtensionInfo.MethodTypeParameters.CastArray<ITypeParameterSymbol>();

        var extensionDeclaration = ExtensionDeclaration()
            .WithTypeParameterList(TypeParameterGenerator.GenerateTypeParameterList(typeParameters, codeGenerationInfo))
            .WithConstraintClauses(typeParameters.GenerateConstraintClauses());


    }
}
