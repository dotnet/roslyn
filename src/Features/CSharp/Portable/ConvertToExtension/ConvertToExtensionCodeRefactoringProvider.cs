// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        var cancellationToken = context.CancellationToken;
        var methodDeclaration = await context.TryGetRelevantNodeAsync<MethodDeclarationSyntax>().ConfigureAwait(false);

        if (methodDeclaration is not { ParameterList.Parameters: [var firstParameter, ..] })
            return;

        if (!firstParameter.Modifiers.Any(SyntaxKind.ThisKeyword))
            return;

        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
            return;


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

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var staticClassDeclarations = GetTopLevelClassDeclarations(root, fixAllSpans);
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
