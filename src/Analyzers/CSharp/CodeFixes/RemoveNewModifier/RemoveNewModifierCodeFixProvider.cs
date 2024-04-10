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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveNew), Shared]
internal class RemoveNewModifierCodeFixProvider : CodeFixProvider
{
    private const string CS0109 = nameof(CS0109); // The member 'SomeClass.SomeMember' does not hide an accessible member. The new keyword is not required.

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public RemoveNewModifierCodeFixProvider()
    {
    }

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override ImmutableArray<string> FixableDiagnosticIds => [CS0109];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var token = root.FindToken(diagnosticSpan.Start);

        var memberDeclarationSyntax = token.GetAncestor<MemberDeclarationSyntax>();
        if (memberDeclarationSyntax == null)
            return;

        var generator = context.Document.GetRequiredLanguageService<SyntaxGenerator>();
        if (!generator.GetModifiers(memberDeclarationSyntax).IsNew)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpCodeFixesResources.Remove_new_modifier,
                ct => FixAsync(context.Document, generator, memberDeclarationSyntax, ct),
                nameof(CSharpCodeFixesResources.Remove_new_modifier)),
            context.Diagnostics);
    }

    private static async Task<Document> FixAsync(
        Document document,
        SyntaxGenerator generator,
        MemberDeclarationSyntax memberDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        return document.WithSyntaxRoot(root.ReplaceNode(
            memberDeclaration,
            generator.WithModifiers(
                memberDeclaration, generator.GetModifiers(memberDeclaration).WithIsNew(false))));
    }
}
