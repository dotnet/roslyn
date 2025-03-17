// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

using FixAllScope = CodeAnalysis.CodeFixes.FixAllScope;

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

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var newDeclaration = await ConvertToExtensionAsync(
            semanticModel, classDeclaration, extensionMethods, cancellationToken).ConfigureAwait(false);

        var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<ClassDeclarationSyntax> ConvertToExtensionAsync(
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
            var newExtension = CreateExtension(group);
            classDeclarationEditor.ReplaceNode(group.First().ExtensionMethod, newExtension);

            foreach (var extensionMethod in group.Skip(1))
                classDeclarationEditor.RemoveNode(extensionMethod.ExtensionMethod);
        }

        return (ClassDeclarationSyntax)classDeclarationEditor.GetChangedRoot();
    }

    private static ExtensionDeclarationSyntax CreateExtension(
        IGrouping<ExtensionMethodInfo, ExtensionMethodInfo> group)
    {
        throw new NotImplementedException();
    }

    private sealed class ConvertToExtensionFixAllProvider()
        : DocumentBasedFixAllProvider(
            [FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution, FixAllScope.ContainingType])
    {
        protected override async Task<Document?> FixAllAsync(
            FixAllContext fixAllContext,
            Document document,
            Optional<ImmutableArray<TextSpan>> fixAllSpans)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            foreach (var declaration in GetTopLevelClassDeclarations(root, fixAllSpans))
            {
                var extensionMethods = GetExtensionMethods(declaration);
                if (extensionMethods.IsEmpty)
                    continue;

                var newDeclaration = await ConvertToExtensionAsync(
                    semanticModel, declaration, extensionMethods, cancellationToken).ConfigureAwait(false);
                editor.ReplaceNode(declaration, newDeclaration);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static IEnumerable<ClassDeclarationSyntax> GetTopLevelClassDeclarations(
            SyntaxNode root, Optional<ImmutableArray<TextSpan>> fixAllSpans)
        {
            if (!fixAllSpans.HasValue)
            {
                // Processing the whole file.  Return all top level classes in the file.
                return root
                    .DescendantNodes(descendIntoChildren: n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                    .OfType<ClassDeclarationSyntax>();
            }
            else
            {
                // User selected 'fix all in containing type'.  Core code refactoring engine will return the spans
                // of the containing class
                return fixAllSpans.Value
                    .Select(span => root.FindNode(span) as ClassDeclarationSyntax)
                    .WhereNotNull();
            }
        }
    }
}
