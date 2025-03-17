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

namespace Microsoft.CodeAnalysis.CSharp.ConvertToExtension;

using FixAllScope = CodeAnalysis.CodeFixes.FixAllScope;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToExtension), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ConvertToExtensionCodeRefactoringProvider() : CodeRefactoringProvider
{
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
        => [.. classDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(m => IsExtensionMethod(m, out _))];

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
            var staticClassDeclarations = GetTopLevelClassDeclarations(root, fixAllSpans)
                .Where(c => c.Modifiers.Any(SyntaxKind.StaticKeyword));

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            foreach (var declaration in staticClassDeclarations)
            {
                var newDeclaration = await ConvertToExtensionAsync(
                    semanticModel, declaration, cancellationToken).ConfigureAwait(false);
                if (newDeclaration != null)
                    editor.ReplaceNode(declaration, newDeclaration);
            }
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
